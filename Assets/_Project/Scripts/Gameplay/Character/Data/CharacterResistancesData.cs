using NaughtyAttributes;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterResistancesData")]
    public class CharacterResistancesData : ScriptableObject
    {
        [field: SerializeField] 
        public float PhysicalResist { get; private set; } = 0.1f;

        [field: SerializeField] 
        public float MagicResist { get; private set; } = 0.25f;

        [field: SerializeField] 
        public float FireResist { get; private set; } = 0.1f;

        [field: SerializeField] 
        public float FaithResist { get; private set; } = 0.15f;
    }
}