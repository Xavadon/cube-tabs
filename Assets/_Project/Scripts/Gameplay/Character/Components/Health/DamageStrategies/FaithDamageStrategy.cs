using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    /// <summary>
    /// Отдельного святого резиста в дизайне больше нет (только физ и маг).
    /// Тип урона оставлен, чтобы не переделывать ассеты способностей,
    /// а считается он по магическому резисту.
    /// </summary>
    public class FaithDamageStrategy : IDamageStrategy
    {
        public float CalculateDamage(float baseDamage, CharacterResistances resistances)
        {
            return baseDamage * (1f - resistances.MagicResist);
        }

        public string GetDamageTypeName() => "Faith";
    }
}
