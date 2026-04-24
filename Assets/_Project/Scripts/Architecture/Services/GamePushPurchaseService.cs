using System;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class GamePushPurchaseService : IPurchaseService
    {
        public UniTask Initialize()
        {
#if !UNITY_EDITOR
            GP_Payments.Fetch(
                onFetchSuccess: () => Debug.Log("[GamePushPurchaseService] Catalog fetched"),
                onFetchError: err => Debug.LogWarning($"[GamePushPurchaseService] Fetch error: {err}")
            );
#endif
            Debug.Log("[GamePushPurchaseService] Initialized");
            return UniTask.CompletedTask;
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
                onPurchaseSuccess: () =>
                {
                    Debug.Log($"[GamePushPurchaseService] Purchase success: {item.YandexProductId}");
                    ConsumeIfNeeded(item, onSuccess);
                },
                onPurchaseError: err =>
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Purchase error: {err}");
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
                onConsumeSuccess: () =>
                {
                    Debug.Log($"[GamePushPurchaseService] Consumed: {item.YandexProductId}");
                    onSuccess?.Invoke();
                },
                onConsumeError: err =>
                {
                    Debug.LogWarning($"[GamePushPurchaseService] Consume error: {err}, but purchase succeeded");
                    onSuccess?.Invoke();
                }
            );
#else
            onSuccess?.Invoke();
#endif
        }
    }
}
