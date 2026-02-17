using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Стратегия расчета магического урона.
    ///
    /// Базовая формула: damage = baseDamage * (1 - magicResist)
    ///
    /// В будущем можно добавить:
    /// - Магический пробой (игнорирует часть сопротивления)
    /// - Усиление от статов (Intelligence)
    /// - Элементальные реакции (если есть огонь/лед и т.д.)
    /// </summary>
    public class MagicDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            // Базовая формула - такая же как у физического урона
            float damage = baseDamage * (1f - resistances.MagicResist);

            // TODO: Добавить магический пробой
            // const float magicPenetration = 0.1f; // 10% пробой
            // float effectiveResist = Mathf.Max(0, resistances.MagicResist - magicPenetration);
            // float damage = baseDamage * (1f - effectiveResist);

            return damage;
        }

        public string GetDamageTypeName() => "Magic";
    }
}
