using _Project.Scripts.Gameplay.Character.Data;

namespace _Project.Scripts.Gameplay.Character.Health.DamageStrategies
{
    public interface IDamageStrategy
    {
        float CalculateDamage(float baseDamage, CharacterResistances resistances);
        string GetDamageTypeName();
    }
    
    public readonly struct CharacterResistances
    {
        public readonly float PhysicalResist;
        public readonly float MagicResist;
        public readonly float FireResist;
        public readonly float FaithResist;
        
        public CharacterResistances(float physical, float magic, float fire, float faith)
        {
            PhysicalResist = physical;
            MagicResist = magic;
            FireResist = fire;
            FaithResist = faith;
        }
        
        public static CharacterResistances FromTierData(TierData tier)
        {
            var stats = tier.Stats;
            return new CharacterResistances(
                stats.PhysicalResist,
                stats.MagicResist,
                stats.FireResist,
                stats.FaithResist
            );
        }
    }
}
