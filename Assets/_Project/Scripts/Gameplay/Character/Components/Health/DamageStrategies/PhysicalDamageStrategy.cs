using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Физический урон. Снижение берётся из брони по дотовской формуле,
    /// считается в StatFormulas.PhysicalReduction: 0.06 * armor / (1 + 0.06 * |armor|).
    /// </summary>
    public class PhysicalDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            return baseDamage * (1f - resistances.PhysicalReduction);
        }

        public string GetDamageTypeName() => "Physical";
    }
}
