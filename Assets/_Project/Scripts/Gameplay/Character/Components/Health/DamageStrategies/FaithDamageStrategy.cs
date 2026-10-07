using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    public class FaithDamageStrategy : IDamageStrategy
    {
        private const float MINIMUM_DAMAGE_PERCENT = 0.3f;

        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            float normalDamage = baseDamage * (1f - resistances.FaithResist);

            float minimumDamage = baseDamage * MINIMUM_DAMAGE_PERCENT;

            float finalDamage = Mathf.Max(normalDamage, minimumDamage);

            if (finalDamage == minimumDamage)
            {
                Debug.Log($"[Faith Damage] Сработал минимальный порог урона: {minimumDamage} " +
                         $"(вместо {normalDamage} из-за высокого сопротивления {resistances.FaithResist * 100}%)");
            }

            return finalDamage;
        }

        public string GetDamageTypeName() => "Faith";
    }
}
