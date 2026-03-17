using System;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [Serializable]
    public class CharacterStatsData
    {
        [field: SerializeField]
        public float Health { get; private set; } = 100f;
        
        [field: SerializeField] [field: Header("Resistances")]
        public float PhysicalResist { get; private set; } 

        [field: SerializeField]
        public float MagicResist { get; private set; }

        [field: SerializeField]
        public float FireResist { get; private set; }

        [field: SerializeField]
        public float FaithResist { get; private set; } 
        
        [field: SerializeField] [field: Header("Damage")] 
        public float Damage { get; private set; } = 20f;

        [field: SerializeField]
        public DamageType DamageType { get; private set; } = DamageType.Physical;
    }
}
