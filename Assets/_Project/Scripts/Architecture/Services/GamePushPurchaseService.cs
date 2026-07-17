using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class GamePushPurchaseService : IPurchaseService
    {
        private readonly Dictionary<string, FetchProducts> _productsByTag = new();

        // Незакрытые покупки платформы (оплачено, ещё не потреблено). Ключ — tag или productId.
        private readonly List<string> _pendingPurchases = new();

        public async UniTask Initialize()
        {
#if !UNITY_EDITOR
            // Ждём инициализации GamePush SDK
            if (!GP_Init.isReady)
            {
                Debug.Log("[GamePushPurchaseService] Waiting for GP_Init.OnReady...");
                var initTcs = new UniTaskCompletionSource();
                GP_Init.OnReady += () => initTcs.TrySetResult();
                await initTcs.Task;
            }

            Debug.Log($"[GamePushPurchaseService] Platform: {GP_Platform.Type()}");

            var tcs = new UniTaskCompletionSource<bool>();

            GP_Payments.Fetch(
                onFetchProducts: products =>
                {
                    Debug.Log($"[GamePushPurchaseService] Products count: {products?.Count ?? 0}");
                    if (products != null)
                    {
                        foreach (var p in products)
                        {
                            Debug.Log($"[GamePushPurchaseService] Product: id={p.id}, tag={p.tag}, name={p.name}, price={p.price}, currency={p.currency}, currencySymbol={p.currencySymbol}");
                            _productsByTag[p.id.ToString()] = p;
                            if (!string.IsNullOrEmpty(p.tag))
                                _productsByTag[p.tag] = p;
                        }
                    }
                    tcs.TrySetResult(true);
                },
                onFetchProductsError: () =>
                {
                    Debug.LogWarning("[GamePushPurchaseService] Fetch error");
                    tcs.TrySetResult(false);
                },
                onFetchPlayerPurchases: purchases =>
                {
                    Debug.Log($"[GamePushPurchaseService] Pending purchases count: {purchases?.Count ?? 0}");
                    CachePendingPurchases(purchases);
                }
            );

            await tcs.Task;
#else
            await UniTask.CompletedTask;
#endif
            Debug.Log("[GamePushPurchaseService] Initialized");
        }

        private void CachePendingPurchases(List<FetchPlayerPurchases> purchases)
        {
            _pendingPurchases.Clear();

            if (purchases == null)
                return;

            foreach (var p in purchases)
            {
                string key = !string.IsNullOrEmpty(p.tag) ? p.tag : p.productId.ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    _pendingPurchases.Add(key);
                    Debug.Log($"[GamePushPurchaseService] Pending: {key}");
                }
            }
        }

        public UniTask RestorePendingPurchases(Func<string, bool> fulfill)
        {
#if !UNITY_EDITOR
            if (_pendingPurchases.Count == 0)
            {
                Debug.Log("[GamePushPurchaseService] No pending purchases to restore");
                return UniTask.CompletedTask;
            }

            foreach (var productId in _pendingPurchases.ToList())
            {
                bool applied = fulfill != null && fulfill(productId);

                if (applied)
                {
                    Debug.Log($"[GamePushPurchaseService] Restored '{productId}', consuming");
                    ConsumeSilent(productId);
                    _pendingPurchases.Remove(productId);
                }
                else
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Pending '{productId}' has no catalog reward, left unconsumed");
                }
            }
#endif
            return UniTask.CompletedTask;
        }

        public string GetPrice(string productId, string fallback)
        {
            if (string.IsNullOrEmpty(productId))
            {
                Debug.LogWarning($"[GamePushPurchaseService] GetPrice: empty productId, using fallback '{fallback}'");
                return fallback;
            }

            if (_productsByTag.TryGetValue(productId, out var product))
            {
                // Валюта берётся автоматически из свойств продукта (SDK), не хардкодится
                // (требование Yandex п.3.8: название/иконка валюты — из IProduct).
                string priceStr = product.price.ToString();
                string symbol = product.currencySymbol;

                string result = string.IsNullOrEmpty(symbol) ? priceStr : $"{priceStr} {symbol}";
                Debug.Log($"[GamePushPurchaseService] GetPrice: found '{productId}' -> {result}");
                return result;
            }

            Debug.LogWarning($"[GamePushPurchaseService] GetPrice: '{productId}' not found in cache ({_productsByTag.Count} products), using fallback '{fallback}'");
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
#else
            Debug.Log($"[GamePushPurchaseService] Purchasing: {item.YandexProductId}");

            GP_Payments.Purchase(
                idOrTag: item.YandexProductId,
                onPurchaseSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Purchase success: {item.YandexProductId}");
                    // Порядок по докам Yandex: сначала выдать награду, потом consume.
                    onSuccess?.Invoke();
                    ConsumeSilent(item.YandexProductId);
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
#if UNITY_EDITOR
            Debug.Log($"[GamePushPurchaseService] Editor mock purchase unit: {unit.Name}");
            onSuccess?.Invoke();
#else
            if (string.IsNullOrEmpty(unit.YandexProductId))
            {
                Debug.LogError($"[GamePushPurchaseService] No YandexProductId for unit: {unit.Name}");
                onFailure?.Invoke();
                return;
            }

            Debug.Log($"[GamePushPurchaseService] Purchasing unit: {unit.YandexProductId}");

            GP_Payments.Purchase(
                idOrTag: unit.YandexProductId,
                onPurchaseSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Unit purchase success: {unit.YandexProductId}");
                    // Сначала выдать награду, потом consume.
                    onSuccess?.Invoke();
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
#endif
        }
    }
}
