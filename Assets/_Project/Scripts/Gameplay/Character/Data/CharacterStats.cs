using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using _Project.Scripts.Gameplay.Inventory;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    public class CharacterStats
    {
        public float Health { get; }
        public float Damage { get; }
        public DamageType DamageType { get; }
        public float PhysicalResist { get; }
        public float MagicResist { get; }
        public float FireResist { get; }
        public float FaithResist { get; }

        public CharacterStats(CharacterStatsData baseStats, StatBonus bonus)
        {
            bonus ??= StatBonus.Empty;

            Health = Mathf.Max(1f, baseStats.Health + bonus.Health);
            Damage = Mathf.Max(0f, baseStats.Damage + bonus.Damage);
            DamageType = baseStats.DamageType;
            PhysicalResist = Mathf.Clamp01(baseStats.PhysicalResist + bonus.PhysicalResist);
            MagicResist = Mathf.Clamp01(baseStats.MagicResist + bonus.MagicResist);
            FireResist = Mathf.Clamp01(baseStats.FireResist + bonus.FireResist);
            FaithResist = Mathf.Clamp01(baseStats.FaithResist + bonus.FaithResist);
        }

        public CharacterResistances ToResistances()
        {
            return new CharacterResistances(PhysicalResist, MagicResist, FireResist, FaithResist);
        }
    }
}
