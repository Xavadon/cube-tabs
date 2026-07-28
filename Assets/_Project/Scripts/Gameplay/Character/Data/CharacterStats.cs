using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using _Project.Scripts.Gameplay.Inventory;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    public class CharacterStats
    {
        public float Strength { get; }
        public float Agility { get; }
        public float Intelligence { get; }

        public float Health { get; }
        public float HealthRegen { get; }
        public float Armor { get; }
        public float PhysicalReduction { get; }
        public float MagicResist { get; }
        public float AttackSpeed { get; }
        public float AttackIntervalMultiplier { get; }
        public float Damage { get; }
        public DamageType DamageType { get; }

        public CharacterStats(CharacterStatsData baseStats, StatBonus bonus)
        {
            bonus ??= StatBonus.Empty;

            Strength = Mathf.Max(0f, baseStats.Strength + bonus.Strength);
            Agility = Mathf.Max(0f, baseStats.Agility + bonus.Agility);
            Intelligence = Mathf.Max(0f, baseStats.Intelligence + bonus.Intelligence);

            Health = Mathf.Max(1f, baseStats.Health + bonus.Health + Strength * StatFormulas.HealthPerStrength);
            HealthRegen = baseStats.HealthRegen + Strength * StatFormulas.HealthRegenPerStrength;

            Armor = baseStats.Armor + bonus.Armor + Agility * StatFormulas.ArmorPerAgility;
            PhysicalReduction = StatFormulas.PhysicalReduction(Armor);

            MagicResist = StatFormulas.ClampMagicResist(
                baseStats.MagicResist + bonus.MagicResist + Intelligence * StatFormulas.MagicResistPerIntelligence);

            AttackSpeed = baseStats.AttackSpeed + bonus.AttackSpeed + Agility * StatFormulas.AttackSpeedPerAgility;
            AttackIntervalMultiplier = StatFormulas.AttackIntervalMultiplier(AttackSpeed);

            Damage = Mathf.Max(0f, baseStats.Damage + bonus.Damage);
            DamageType = baseStats.DamageType;
        }

        public CharacterResistances ToResistances()
        {
            return new CharacterResistances(PhysicalReduction, MagicResist);
        }
    }
}
