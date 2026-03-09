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
        public EvolutionCatalog EvolutionCatalog { get; private set; }

        [field: SerializeField]
        public int BaseArmySlots { get; private set; } = 3;

        [field: SerializeField]
        public int SlotUpgradeCost { get; private set; } = 200;

        [field: SerializeField]
        public int MaxArmySlots { get; private set; } = 8;

        [field: SerializeField]
        public int StartingGold { get; private set; } = 300;

        public CharacterData GetUnitById(int id)
        {
            foreach (var unit in AvailableUnits)
            {
                if (unit.Id == id)
                    return unit;
            }

            return null;
        }
    }
}
