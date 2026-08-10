using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Inventory;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    [CreateAssetMenu(menuName = "Config/Arpg/Config")]
    public class ArpgConfig : ScriptableObject
    {
        public const string ResourcePath = "Data/ArpgConfig";

        [field: SerializeField]
        public string StartScene { get; private set; } = "Frog Swamp";

        [field: Header("Player")]
        [field: SerializeField]
        public CharacterData PlayerUnit { get; private set; }

        [field: SerializeField]
        public int PlayerTier { get; private set; }

        // ТЕСТ: выдаётся при старте, убрать вместе с песочницей
        [field: Header("Test")]
        [field: SerializeField]
        public int StartingGold { get; private set; } = 1000;

        [field: SerializeField]
        public StatBonus StartingStats { get; private set; } = new();

        [field: SerializeField]
        public ItemData[] StartingItems { get; private set; }

        [field: SerializeField]
        public ItemData[] StartingBackpack { get; private set; }
    }
}
