using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    public enum ShopItemRewardType
    {
        Gold,
        ArmySlot
    }

    [CreateAssetMenu(menuName = "Config/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public string YandexProductId { get; private set; }

        [field: SerializeField]
        public Sprite Icon { get; private set; }

        [field: SerializeField, TextArea(2, 4)]
        public string Description { get; private set; }

        [field: SerializeField]
        public string PriceLabel { get; private set; }

        [field: SerializeField]
        public ShopItemRewardType RewardType { get; private set; }

        [field: SerializeField]
        public int RewardAmount { get; private set; }
    }
}
