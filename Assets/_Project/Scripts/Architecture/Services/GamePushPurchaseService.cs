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
        private const string YanCurrency = "YAN";

        private readonly ISaveService _saveService;

        private readonly Dictionary<string, FetchProducts> _productsByKey = new();

        private readonly List<FetchPlayerPurchases> _pendingPurchases = new();

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

            GP_Payments.OnFetchProducts += HandleProducts;
            GP_Payments.OnFetchPlayerPurchases += HandlePurchases;
            GP_Payments.OnFetchProductsError += HandleProductsError;

            try
            {
                GP_Payments.Fetch();

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
                    Debug.Log($"[GamePushPurchaseService] Permanent purchase '{consumeKey}' kept");
                    continue;
                }

                if (result == PurchaseFulfillResult.Unknown)
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Pending '{consumeKey}' не найден в каталоге — консумим всё равно, " +
                                     "необработанных платежей оставаться не должно (Yandex п.1.13.1)");
                }

                _saveService.ForceSync();
                ConsumeSilent(consumeKey);
                _pendingPurchases.Remove(purchase);
            }
        }

        private static string GetConsumeKey(FetchPlayerPurchases purchase)
        {
            return !string.IsNullOrEmpty(purchase.tag)
                ? purchase.tag
                : purchase.productId.ToString();
        }

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

        public string GetPrice(string productId)
        {
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogWarning("[GamePushPurchaseService] GetPrice: empty productId");
                return string.Empty;
            }

            if (_productsByKey.TryGetValue(productId, out var product))
            {
                string priceStr = product.price.ToString();
                string result = $"{priceStr} {GetCurrencyLabel(product)}".TrimEnd();

                Debug.Log($"[GamePushPurchaseService] GetPrice: found '{productId}' -> {result} " +
                          $"(currency='{product.currency}', symbol='{product.currencySymbol}')");
                return result;
            }

            Debug.LogWarning($"[GamePushPurchaseService] GetPrice: '{productId}' not found in cache ({_productsByKey.Count} products)");
            return string.Empty;
        }

        private static string GetCurrencyLabel(FetchProducts product)
        {
            if (string.Equals(product.currency, YanCurrency, StringComparison.OrdinalIgnoreCase))
                return PriceTags.Yan;

            return string.IsNullOrEmpty(product.currencySymbol) ? product.currency : product.currencySymbol;
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
            bool consumable = item.RewardType != ShopItemRewardType.NoAds;

            Debug.Log($"[GamePushPurchaseService] Purchasing: {item.YandexProductId}");

            GP_Payments.Purchase(
                idOrTag: item.YandexProductId,
                onPurchaseSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Purchase success: {item.YandexProductId}");

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
