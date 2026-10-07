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
            var stats = tier.Stats;
            _physicalResist = stats.PhysicalResist;
            _magicResist = stats.MagicResist;
            _fireResist = stats.FireResist;
            _faithResist = stats.FaithResist;
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
