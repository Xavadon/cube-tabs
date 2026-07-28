using System;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [Serializable]
    public class CharacterStatsData
    {
        [field: SerializeField] [field: Header("Attributes")]
        public float Strength { get; private set; }

        [field: SerializeField]
        public float Agility { get; private set; }

        [field: SerializeField]
        public float Intelligence { get; private set; }

        [field: SerializeField] [field: Header("Base")]
        public float Health { get; private set; } = 100f;

        [field: SerializeField]
        public float HealthRegen { get; private set; }

        [field: SerializeField]
        public float Mana { get; private set; }

        [field: SerializeField]
        public float ManaRegen { get; private set; }

        [field: SerializeField]
        public float Armor { get; private set; }

        [field: SerializeField]
        public float MagicResist { get; private set; } = 0.25f;

        [field: SerializeField]
        public float AttackSpeed { get; private set; } = 100f;

        [field: SerializeField] [field: Header("Damage")]
        public float Damage { get; private set; } = 20f;

        [field: SerializeField]
        public DamageType DamageType { get; private set; } = DamageType.Physical;
    }
}
