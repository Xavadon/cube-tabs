using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    public class MagicDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
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
