namespace _Project.Scripts.Gameplay.Character.Health.DamageStrategies
{
    public interface IDamageStrategy
    {
        float CalculateDamage(float baseDamage, CharacterResistances resistances);
        string GetDamageTypeName();
    }

    public readonly struct CharacterResistances
    {
        public readonly float PhysicalReduction;
        public readonly float MagicResist;

        public CharacterResistances(float physicalReduction, float magicResist)
        {
            PhysicalReduction = physicalReduction;
            MagicResist = magicResist;
        }
    }
}
