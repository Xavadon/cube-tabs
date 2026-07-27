using System;
using _Project.Scripts.Gameplay.Inventory;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Merchant.Data
{
    [Serializable]
    public class MerchantEntry
    {
        [field: SerializeField]
        public ItemData Item { get; private set; }

        [field: SerializeField]
        public int Price { get; private set; } = 50;
    }

    [CreateAssetMenu(menuName = "Config/Merchant/Stock")]
    public class MerchantStockData : ScriptableObject
    {
        [field: SerializeField]
        public MerchantEntry[] Entries { get; private set; }
    }
}
