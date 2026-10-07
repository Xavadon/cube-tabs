using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    public class FireDamageStrategy : IDamageStrategy
    {
        private const float FIRE_PENETRATION = 0.2f;

        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            float effectiveResist = resistances.FireResist * (1f - FIRE_PENETRATION);

            float damage = baseDamage * (1f - effectiveResist);

            // TODO: Добавить DoT эффект
            // if (shouldApplyBurning)
            // {
            //     ApplyBurningEffect(target, baseDamage * 0.2f, duration: 3f);
            // }

            return damage;
        }

        public string GetDamageTypeName() => "Fire";
    }
}
