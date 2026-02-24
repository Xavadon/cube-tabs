using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField]
        public int Id { get; private set; } = 0;
        
        [field: SerializeField]
        public string Name { get; private set; } = "DefaultCharacter";
        
        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;
        
        [field: SerializeField]
        public CharacterStatsData CharacterStatsData { get; private set; }
        
        [field: SerializeField]
        public CharacterResistancesData CharacterResistancesData { get; private set; }
        
        [field: SerializeField]
        public BrainDataBase BrainData { get; private set; }
        
        [Header("Equipment")]
        public WeaponData[] WeaponData;
    }
}