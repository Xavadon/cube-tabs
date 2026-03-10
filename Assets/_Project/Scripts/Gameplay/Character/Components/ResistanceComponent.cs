using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies;

namespace Game.Scripts.Core.Gameplay.Enemies.Components
{
    //TODO: refactor
    public class ResistanceComponent
    {
        private float _physicalResist;
        private float _magicResist;
        private float _fireResist;
        private float _faithResist;

        public ResistanceComponent(TierData tier)
        {
            _physicalResist = tier.Resistances.PhysicalResist;
            _magicResist = tier.Resistances.MagicResist;
            _fireResist = tier.Resistances.FireResist;
            _faithResist = tier.Resistances.FaithResist;
        }

        public void SetResistance(DamageType type, float value)
        {
            switch (type)
            {
                case DamageType.Physical:
                    _physicalResist = value;
                    break;
                case DamageType.Magic:
                    _magicResist = value;
                    break;
                case DamageType.Fire:
                    _fireResist = value;
                    break;
                case DamageType.Faith:
                    _faithResist = value;
                    break;
            }
        }

        public float GetResistance(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => _physicalResist,
                DamageType.Magic => _magicResist,
                DamageType.Fire => _fireResist,
                DamageType.Faith => _faithResist,
                _ => 0f
            };
        }

        public CharacterResistances GetResistances()
        {
            return new CharacterResistances(_physicalResist, _magicResist, _fireResist, _faithResist);
        }
    }
}
