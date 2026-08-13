using System;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    /// <summary>
    /// Что делать с незакрытой покупкой платформы после попытки выдать награду.
    /// </summary>
    public enum PurchaseFulfillResult
    {
        /// <summary>Покупка не сопоставлена ни с одной наградой каталога.</summary>
        Unknown = 0,

        /// <summary>Расходуемая покупка: награда выдана, платёж нужно потребить.</summary>
        Consume,

        /// <summary>
        /// Постоянная покупка (NoAds): консумить нельзя — сам факт её наличия в списке покупок
        /// платформы и есть признак владения (Yandex: постоянные обрабатываются через getPurchases).
        /// </summary>
        Keep
    }

    public interface IPurchaseService : IService
    {
        void Purchase(ShopItemData item, Action onSuccess, Action onFailure);
        void Purchase(CharacterData unit, Action onSuccess, Action onFailure);
        string GetPrice(string productId);

        /// <summary>
        /// Обрабатывает незакрытые покупки платформы (оплачено, но не выдано/не потреблено —
        /// например вкладку закрыли между оплатой и consume). Для каждой вызывает fulfill(productId).
        /// Расходуемые консумятся всегда, даже если награда не нашлась: необработанных платежей
        /// оставаться не должно (Yandex п.1.13.1).
        /// </summary>
        UniTask RestorePendingPurchases(Func<string, PurchaseFulfillResult> fulfill);
    }
}
