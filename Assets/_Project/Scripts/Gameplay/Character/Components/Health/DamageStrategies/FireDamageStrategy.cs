using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Стратегия расчета урона от огня.
    ///
    /// Особенности огненного урона:
    /// - Огонь частично игнорирует сопротивление (20% пробой)
    /// - Это делает огонь эффективным против бронированных врагов
    ///
    /// Формула: damage = baseDamage * (1 - effectiveResist)
    /// где effectiveResist = fireResist * 0.8 (огонь игнорирует 20% сопротивления)
    ///
    /// Пример:
    /// - Базовый урон: 100
    /// - Огненное сопротивление: 0.5 (50%)
    /// - Эффективное сопротивление: 0.5 * 0.8 = 0.4 (40%)
    /// - Итоговый урон: 100 * (1 - 0.4) = 60 (вместо 50 без пробоя)
    ///
    /// В будущем можно добавить:
    /// - DoT эффект (горение)
    /// - Распространение огня на соседних врагов
    /// - Бонусный урон по замороженным врагам
    /// </summary>
    public class FireDamageStrategy : IDamageStrategy
    {
        // Огонь игнорирует 20% сопротивления
        private const float FIRE_PENETRATION = 0.2f;

        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            // Огонь уменьшает эффективность сопротивления
            float effectiveResist = resistances.FireResist * (1f - FIRE_PENETRATION);

            // Применяем уменьшенное сопротивление
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
