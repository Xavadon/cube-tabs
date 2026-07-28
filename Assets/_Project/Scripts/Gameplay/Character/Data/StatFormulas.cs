using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    public static class StatFormulas
    {
        public const float HealthPerStrength = 22f;
        public const float HealthRegenPerStrength = 0.1f;

        public const float ArmorPerAgility = 0.2f;
        public const float AttackSpeedPerAgility = 1f;

        public const float ManaPerIntelligence = 12f;
        public const float ManaRegenPerIntelligence = 0.05f;
        public const float MagicResistPerIntelligence = 0.001f;

        private const float ArmorConstant = 0.06f;
        private const float MaxMagicResist = 0.9f;

        public static float PhysicalReduction(float armor)
        {
            float scaled = ArmorConstant * armor;
            return scaled / (1f + ArmorConstant * Mathf.Abs(armor));
        }

        public static float ClampMagicResist(float value)
        {
            return Mathf.Min(value, MaxMagicResist);
        }

        public static float AttackIntervalMultiplier(float attackSpeed)
        {
            return 100f / Mathf.Max(20f, attackSpeed);
        }
    }
}
