using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField]
        public string Name { get; private set; }
        
        [field: SerializeField]
        public float MoveSpeed { get; private set; }
        
        [field: SerializeField]
        public CharacterStatsData CharacterStatsData { get; private set; }
        
        [field: SerializeField]
        public CharacterResistancesData CharacterResistancesData { get; private set; }
        
        [Header("Equipment")]
        public WeaponData[] WeaponData;
    }
}