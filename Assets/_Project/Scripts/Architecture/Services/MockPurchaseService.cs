using System;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class MockPurchaseService : IPurchaseService
    {
        public UniTask Initialize()
        {
            Debug.Log("[MockPurchaseService] Initialized (mock — purchases succeed instantly)");
            return UniTask.CompletedTask;
        }

        public void Purchase(ShopItemData item, Action onSuccess, Action onFailure)
        {
            Debug.Log($"[MockPurchaseService] Mock purchase: {item.Name} ({item.PriceLabel})");
            onSuccess?.Invoke();
        }
    }
}
