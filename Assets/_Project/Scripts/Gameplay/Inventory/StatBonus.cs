using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Inventory
{
    [Serializable]
    public class StatBonus
    {
        public static readonly StatBonus Empty = new();

        [field: SerializeField]
        public float Health { get; private set; }

        [field: SerializeField]
        public float Damage { get; private set; }

        [field: SerializeField] [field: Header("Resistances")]
        public float PhysicalResist { get; private set; }

        [field: SerializeField]
        public float MagicResist { get; private set; }

        [field: SerializeField]
        public float FireResist { get; private set; }

        [field: SerializeField]
        public float FaithResist { get; private set; }

        public static StatBonus operator +(StatBonus a, StatBonus b)
        {
            if (a == null)
                return b ?? Empty;

            if (b == null)
                return a;

            return new StatBonus
            {
                Health = a.Health + b.Health,
                Damage = a.Damage + b.Damage,
                PhysicalResist = a.PhysicalResist + b.PhysicalResist,
                MagicResist = a.MagicResist + b.MagicResist,
                FireResist = a.FireResist + b.FireResist,
                FaithResist = a.FaithResist + b.FaithResist
            };
        }
    }
}
