using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/ShopCatalog")]
    public class ShopCatalog : ScriptableObject
    {
        [field: SerializeField]
        public CharacterData[] AvailableUnits { get; private set; }

        [field: SerializeField]
        public CharacterData BaseUnit { get; private set; }

        [field: SerializeField]
        public CharacterData StartingUnit { get; private set; }

        [field: SerializeField]
        public EvolutionCatalog EvolutionCatalog { get; private set; }

        [field: SerializeField]
        public int BaseArmySlots { get; private set; } = 3;

        [field: SerializeField]
        public int SlotUpgradeCost { get; private set; } = 200;

        [field: SerializeField]
        public int MaxArmySlots { get; private set; } = 8;

        [field: SerializeField]
        public int StartingGold { get; private set; } = 300;

        [field: SerializeField]
        public CharacterData[] UniqueHeroes { get; private set; }

        [field: SerializeField]
        public ShopItemData[] ShopItems { get; private set; }

        public CharacterData GetUnitById(string id)
        {
            foreach (var unit in AvailableUnits)
            {
                if (unit.Id == id)
                    return unit;
            }

            if (UniqueHeroes != null)
            {
                foreach (var unit in UniqueHeroes)
                {
                    if (unit.Id == id)
                        return unit;
                }
            }

            return null;
        }
    }
}
