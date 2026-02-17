using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/CharacterBrain")]
    public class AiBrainData : ScriptableObject
    {
        [field: SerializeField] 
        public float DetectionRadius { get; private set; } = 10f;

        [field: SerializeField] 
        public float AttackRange { get; private set; } = 2f;
        
        [field: SerializeField]
        public float AttackCooldown { get; private set; } = 1.5f;
    }
}
