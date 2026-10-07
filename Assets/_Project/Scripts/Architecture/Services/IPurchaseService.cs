using System;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    public enum PurchaseFulfillResult
    {
        Unknown = 0,

        Consume,

        Keep
    }

    public interface IPurchaseService : IService
    {
        void Purchase(ShopItemData item, Action onSuccess, Action onFailure);
        void Purchase(CharacterData unit, Action onSuccess, Action onFailure);
        string GetPrice(string productId);

        UniTask RestorePendingPurchases(Func<string, PurchaseFulfillResult> fulfill);
    }
}
