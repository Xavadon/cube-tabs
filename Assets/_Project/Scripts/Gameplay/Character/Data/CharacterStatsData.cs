using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterStatsData")]
    public class CharacterStatsData : ScriptableObject
    {
        [field: SerializeField]
        public float Health { get; private set; } = 100f;
        
        [field: SerializeField]
        public float Mana { get; private set; } = 100f;

        [field: SerializeField] 
        public float Stamina { get; private set; } = 100f;
    }
}