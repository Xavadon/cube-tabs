using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Стратегия расчета физического урона.
    ///
    /// Базовая формула: damage = baseDamage * (1 - physicalResist)
    ///
    /// Пример:
    /// - Базовый урон: 100
    /// - Физическое сопротивление: 0.2 (20%)
    /// - Итоговый урон: 100 * (1 - 0.2) = 80
    /// </summary>
    public class PhysicalDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            // Простая формула: урон уменьшается на процент сопротивления
            float damage = baseDamage * (1f - resistances.PhysicalResist);

            return damage;
        }

        public string GetDamageTypeName() => "Physical";
    }
}
