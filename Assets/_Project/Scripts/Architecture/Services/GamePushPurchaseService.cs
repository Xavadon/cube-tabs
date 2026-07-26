using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Architecture.Services.Save;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class GamePushPurchaseService : IPurchaseService
    {
        private const int FetchTimeoutSeconds = 10;

        private readonly ISaveService _saveService;

        // Ключ — и id товара, и его tag: в дашборде GamePush это разные поля, а в каталоге игры
        // (ShopItemData.YandexProductId) может лежать любое из них.
        private readonly Dictionary<string, FetchProducts> _productsByKey = new();

        // Незакрытые покупки платформы (оплачено, ещё не потреблено).
        private readonly List<FetchPlayerPurchases> _pendingPurchases = new();

        // Preserve — задачу ждут дважды: сам Initialize и RestorePendingPurchases.
        private UniTask _fetchTask;
        private bool _fetchStarted;

        public GamePushPurchaseService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public UniTask Initialize()
        {
            if (!_fetchStarted)
            {
                _fetchStarted = true;
                _fetchTask = FetchAsync().Preserve();
            }

            return _fetchTask;
        }

        private async UniTask FetchAsync()
        {
#if !UNITY_EDITOR
            // Ждём инициализации GamePush SDK
            if (!GP_Init.isReady)
            {
                Debug.Log("[GamePushPurchaseService] Waiting for GP_Init.OnReady...");
                var initTcs = new UniTaskCompletionSource();
                void OnReady()
                {
                    GP_Init.OnReady -= OnReady;
                    initTcs.TrySetResult();
                }
                GP_Init.OnReady += OnReady;
                await initTcs.Task;
            }

            Debug.Log($"[GamePushPurchaseService] Platform: {GP_Platform.Type()}");

            var productsTcs = new UniTaskCompletionSource();
            var purchasesTcs = new UniTaskCompletionSource();

            void HandleProducts(List<FetchProducts> products)
            {
                CacheProducts(products);
                productsTcs.TrySetResult();
            }

            void HandlePurchases(List<FetchPlayerPurchases> purchases)
            {
                CachePendingPurchases(purchases);
                purchasesTcs.TrySetResult();
            }

            void HandleProductsError()
            {
                Debug.LogWarning("[GamePushPurchaseService] Fetch error");
                productsTcs.TrySetResult();
            }

            // Подписка на статические события, а не на колбэки Fetch: Fetch делегаты присваивает,
            // и параллельный вызов Fetch из самого GP_Payments.Start() их затирает — тогда наши
            // колбэки не приходят вообще и инициализация виснет.
            GP_Payments.OnFetchProducts += HandleProducts;
            GP_Payments.OnFetchPlayerPurchases += HandlePurchases;
            GP_Payments.OnFetchProductsError += HandleProductsError;

            try
            {
                GP_Payments.Fetch();

                // Список покупок приходит отдельным сообщением от списка товаров — ждём оба,
                // иначе RestorePendingPurchases увидит пустой список и ничего не потребит.
                var fetched = UniTask.WhenAll(productsTcs.Task, purchasesTcs.Task);
                var timeout = UniTask.Delay(TimeSpan.FromSeconds(FetchTimeoutSeconds), DelayType.Realtime);

                int finished = await UniTask.WhenAny(fetched, timeout);

                if (finished != 0)
                    Debug.LogWarning($"[GamePushPurchaseService] Fetch timeout ({FetchTimeoutSeconds}s), продолжаем без полного ответа платформы");
            }
            finally
            {
                GP_Payments.OnFetchProducts -= HandleProducts;
                GP_Payments.OnFetchPlayerPurchases -= HandlePurchases;
                GP_Payments.OnFetchProductsError -= HandleProductsError;
            }
#else
            await UniTask.CompletedTask;
#endif
            Debug.Log($"[GamePushPurchaseService] Initialized. Products: {_productsByKey.Count}, pending: {_pendingPurchases.Count}");
        }

        private void CacheProducts(List<FetchProducts> products)
        {
            _productsByKey.Clear();

            if (products == null)
                return;

            foreach (var p in products)
            {
                Debug.Log($"[GamePushPurchaseService] Product: id={p.id}, tag={p.tag}, name={p.name}, price={p.price}, currency={p.currency}");

                _productsByKey[p.id.ToString()] = p;

                if (!string.IsNullOrEmpty(p.tag))
                    _productsByKey[p.tag] = p;
            }
        }

        private void CachePendingPurchases(List<FetchPlayerPurchases> purchases)
        {
            _pendingPurchases.Clear();

            if (purchases == null)
                return;

            foreach (var p in purchases)
            {
                if (p == null)
                    continue;

                _pendingPurchases.Add(p);
                Debug.Log($"[GamePushPurchaseService] Pending: tag={p.tag}, productId={p.productId}, subscribed={p.subscribed}");
            }
        }

        public async UniTask RestorePendingPurchases(Func<string, PurchaseFulfillResult> fulfill)
        {
            // Порядок Initialize() сервисов в DI не гарантирован, поэтому ждём фетч сами:
            // иначе список незакрытых покупок пуст и ничего не консумится.
            await Initialize();

            if (_pendingPurchases.Count == 0)
            {
                Debug.Log("[GamePushPurchaseService] No pending purchases to restore");
                return;
            }

            foreach (var purchase in _pendingPurchases.ToList())
            {
                string consumeKey = GetConsumeKey(purchase);

                if (purchase.subscribed)
                {
                    // Подписка не потребляется — она живёт до expiredAt.
                    Debug.Log($"[GamePushPurchaseService] Subscription '{consumeKey}' skipped");
                    continue;
                }

                var result = PurchaseFulfillResult.Unknown;

                foreach (string key in ResolveProductKeys(purchase))
                {
                    result = fulfill != null ? fulfill(key) : PurchaseFulfillResult.Unknown;

                    if (result != PurchaseFulfillResult.Unknown)
                    {
                        Debug.Log($"[GamePushPurchaseService] Restored '{key}' -> {result}");
                        break;
                    }
                }

                if (result == PurchaseFulfillResult.Keep)
                {
                    // Постоянная покупка: она и есть признак владения, потреблять нельзя.
                    Debug.Log($"[GamePushPurchaseService] Permanent purchase '{consumeKey}' kept");
                    continue;
                }

                if (result == PurchaseFulfillResult.Unknown)
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Pending '{consumeKey}' не найден в каталоге — консумим всё равно, " +
                                     "необработанных платежей оставаться не должно (Yandex п.1.13.1)");
                }

                // Данные игрока уже изменены — пушим их в облако до consume, потом потребляем.
                _saveService.ForceSync();
                ConsumeSilent(consumeKey);
                _pendingPurchases.Remove(purchase);
            }
        }

        /// <summary>
        /// Ключ для GP_Payments.Consume — принимает id или tag.
        /// </summary>
        private static string GetConsumeKey(FetchPlayerPurchases purchase)
        {
            return !string.IsNullOrEmpty(purchase.tag)
                ? purchase.tag
                : purchase.productId.ToString();
        }

        /// <summary>
        /// Кандидаты для сопоставления с YandexProductId из каталога игры. Покупка приходит с tag и
        /// числовым productId GamePush, а в каталоге может стоять любой из них — пробуем все.
        /// </summary>
        private IEnumerable<string> ResolveProductKeys(FetchPlayerPurchases purchase)
        {
            var keys = new List<string>();

            void Add(string key)
            {
                if (!string.IsNullOrEmpty(key) && !keys.Contains(key))
                    keys.Add(key);
            }

            Add(purchase.tag);
            Add(purchase.productId.ToString());

            if (_productsByKey.TryGetValue(purchase.productId.ToString(), out var product))
            {
                Add(product.tag);
                Add(product.id.ToString());
            }

            return keys;
        }

        public string GetPrice(string productId, string fallback)
        {
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogWarning($"[GamePushPurchaseService] GetPrice: empty productId, using fallback '{fallback}'");
                return fallback;
            }

            if (_productsByKey.TryGetValue(productId, out var product))
            {
                // Валюта берётся автоматически из свойств продукта (SDK), не хардкодится
                // (требование Yandex п.3.8: название/иконка валюты — из IProduct).
                string priceStr = product.price.ToString();
                string symbol = product.currencySymbol;

                string result = string.IsNullOrEmpty(symbol) ? priceStr : $"{priceStr} {symbol}";
                Debug.Log($"[GamePushPurchaseService] GetPrice: found '{productId}' -> {result}");
                return result;
            }

            Debug.LogWarning($"[GamePushPurchaseService] GetPrice: '{productId}' not found in cache ({_productsByKey.Count} products), using fallback '{fallback}'");
            return fallback;
        }

        public void Purchase(ShopItemData item, Action onSuccess, Action onFailure)
        {
            if (string.IsNullOrEmpty(item.YandexProductId))
            {
                Debug.LogError($"[GamePushPurchaseService] No YandexProductId for item: {item.Name}");
                onFailure?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[GamePushPurchaseService] Editor mock purchase: {item.Name}");
            onSuccess?.Invoke();
            _saveService.ForceSync();
#else
            // NoAds — постоянная покупка, потреблять её нельзя (иначе владение теряется).
            bool consumable = item.RewardType != ShopItemRewardType.NoAds;

            Debug.Log($"[GamePushPurchaseService] Purchasing: {item.YandexProductId}");

            GP_Payments.Purchase(
                idOrTag: item.YandexProductId,
                onPurchaseSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Purchase success: {item.YandexProductId}");

                    // Порядок по докам Yandex: выдать награду -> сохранить игрока -> потребить покупку.
                    onSuccess?.Invoke();
                    _saveService.ForceSync();

                    if (consumable)
                        ConsumeSilent(item.YandexProductId);
                    else
                        Debug.Log($"[GamePushPurchaseService] Permanent purchase '{item.YandexProductId}', consume skipped");
                },
                onPurchaseError: () =>
                {
                    Debug.LogWarning("[GamePushPurchaseService] Purchase error");
                    onFailure?.Invoke();
                }
            );
#endif
        }

        public void Purchase(CharacterData unit, Action onSuccess, Action onFailure)
        {
            if (string.IsNullOrEmpty(unit.YandexProductId))
            {
                Debug.LogError($"[GamePushPurchaseService] No YandexProductId for unit: {unit.Name}");
                onFailure?.Invoke();
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"[GamePushPurchaseService] Editor mock purchase unit: {unit.Name}");
            onSuccess?.Invoke();
            _saveService.ForceSync();
#else
            Debug.Log($"[GamePushPurchaseService] Purchasing unit: {unit.YandexProductId}");

            GP_Payments.Purchase(
                idOrTag: unit.YandexProductId,
                onPurchaseSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Unit purchase success: {unit.YandexProductId}");

                    onSuccess?.Invoke();
                    _saveService.ForceSync();
                    ConsumeSilent(unit.YandexProductId);
                },
                onPurchaseError: () =>
                {
                    Debug.LogWarning("[GamePushPurchaseService] Unit purchase error");
                    onFailure?.Invoke();
                }
            );
#endif
        }

        private void ConsumeSilent(string productId)
        {
#if !UNITY_EDITOR
            GP_Payments.Consume(
                idOrTag: productId,
                onConsumeSuccess: _ => Debug.Log($"[GamePushPurchaseService] Consumed: {productId}"),
                onConsumeError: () => Debug.LogWarning($"[GamePushPurchaseService] Consume error: {productId}")
            );
#else
            Debug.Log($"[GamePushPurchaseService] Editor consume skipped: {productId}");
#endif
        }
    }
}
