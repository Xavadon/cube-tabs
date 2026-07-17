using System;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    public interface IPurchaseService : IService
    {
        void Purchase(ShopItemData item, Action onSuccess, Action onFailure);
        void Purchase(CharacterData unit, Action onSuccess, Action onFailure);
        string GetPrice(string productId, string fallback);

        /// <summary>
        /// Обрабатывает незакрытые покупки платформы (оплачено, но не выдано/не потреблено —
        /// например вкладку закрыли между оплатой и consume). Для каждой вызывает fulfill(productId):
        /// если вернул true (награда выдана) — покупка консумится. Требование Yandex п.1.13.1 / 1.13.5.
        /// </summary>
        UniTask RestorePendingPurchases(Func<string, bool> fulfill);
    }
}
