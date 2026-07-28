using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Inventory
{
    [Serializable]
    public class StatBonus
    {
        public static readonly StatBonus Empty = new();

        [field: SerializeField] [field: Header("Attributes")]
        public float Strength { get; private set; }

        [field: SerializeField]
        public float Agility { get; private set; }

        [field: SerializeField]
        public float Intelligence { get; private set; }

        [field: SerializeField] [field: Header("Flat")]
        public float Damage { get; private set; }

        [field: SerializeField]
        public float Health { get; private set; }

        [field: SerializeField]
        public float Armor { get; private set; }

        [field: SerializeField]
        public float AttackSpeed { get; private set; }

        [field: SerializeField]
        public float MagicResist { get; private set; }

        public static StatBonus operator +(StatBonus a, StatBonus b)
        {
            if (a == null)
                return b ?? Empty;

            if (b == null)
                return a;

            return new StatBonus
            {
                Strength = a.Strength + b.Strength,
                Agility = a.Agility + b.Agility,
                Intelligence = a.Intelligence + b.Intelligence,
                Damage = a.Damage + b.Damage,
                Health = a.Health + b.Health,
                Armor = a.Armor + b.Armor,
                AttackSpeed = a.AttackSpeed + b.AttackSpeed,
                MagicResist = a.MagicResist + b.MagicResist
            };
        }
    }
}
