using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class GamePushPurchaseService : IPurchaseService
    {
        private readonly Dictionary<string, FetchProducts> _productsByTag = new();

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
                            Debug.Log($"[GamePushPurchaseService] Product: id={p.id}, tag={p.tag}, name={p.name}, price={p.price}");
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
                    Debug.Log($"[GamePushPurchaseService] Purchases count: {purchases?.Count ?? 0}");
                }
            );

            await tcs.Task;
#else
            await UniTask.CompletedTask;
#endif
            Debug.Log("[GamePushPurchaseService] Initialized");
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
                Debug.Log($"[GamePushPurchaseService] GetPrice: found '{productId}' -> {product.price}");
                return product.price.ToString();
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
                    ConsumeIfNeeded(item, onSuccess);
                },
                onPurchaseError: () =>
                {
                    Debug.LogWarning("[GamePushPurchaseService] Purchase error");
                    onFailure?.Invoke();
                }
            );
#endif
        }

        private void ConsumeIfNeeded(ShopItemData item, Action onSuccess)
        {
#if !UNITY_EDITOR
            // Consumable товары (золото, слоты) нужно "потребить"
            GP_Payments.Consume(
                idOrTag: item.YandexProductId,
                onConsumeSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Consumed: {item.YandexProductId}");
                    onSuccess?.Invoke();
                },
                onConsumeError: () =>
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Consume error for {item.YandexProductId}, but purchase succeeded");
                    onSuccess?.Invoke();
                }
            );
#else
            onSuccess?.Invoke();
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
                    ConsumeUnit(unit, onSuccess);
                },
                onPurchaseError: () =>
                {
                    Debug.LogWarning("[GamePushPurchaseService] Unit purchase error");
                    onFailure?.Invoke();
                }
            );
#endif
        }

        private void ConsumeUnit(CharacterData unit, Action onSuccess)
        {
#if !UNITY_EDITOR
            GP_Payments.Consume(
                idOrTag: unit.YandexProductId,
                onConsumeSuccess: _ =>
                {
                    Debug.Log($"[GamePushPurchaseService] Consumed unit: {unit.YandexProductId}");
                    onSuccess?.Invoke();
                },
                onConsumeError: () =>
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Consume unit error for {unit.YandexProductId}, but purchase succeeded");
                    onSuccess?.Invoke();
                }
            );
#else
            onSuccess?.Invoke();
#endif
        }
    }
}
