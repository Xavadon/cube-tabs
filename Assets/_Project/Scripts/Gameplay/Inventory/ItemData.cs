using UnityEngine;

namespace _Project.Scripts.Gameplay.Inventory
{
    [CreateAssetMenu(menuName = "Config/Inventory/Item")]
    public class ItemData : ScriptableObject
    {
        [field: SerializeField]
        public string Id { get; private set; }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public Sprite Icon { get; private set; }

        [field: SerializeField]
        public int Tier { get; private set; } = 1;

        [field: SerializeField]
        public StatBonus Bonus { get; private set; } = new();

        public string DisplayName => string.IsNullOrEmpty(Name) ? name : Name;
    }
}
