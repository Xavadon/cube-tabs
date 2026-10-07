using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    public class PhysicalDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            float damage = baseDamage * (1f - resistances.PhysicalResist);

            return damage;
        }

        public string GetDamageTypeName() => "Physical";
    }
}
