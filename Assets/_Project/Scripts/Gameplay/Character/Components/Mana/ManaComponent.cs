using System;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Mana
{
    public class ManaComponent
    {
        private float _maxMana;
        private float _currentMana;
        private float _regenPerSecond;

        public float CurrentMana => _currentMana;
        public float MaxMana => _maxMana;
        public float ManaRatio => _maxMana > 0f ? _currentMana / _maxMana : 0f;
        public float RegenPerSecond => _regenPerSecond;

        public event Action<float, float> OnManaChanged;

        public ManaComponent(CharacterStats stats)
        {
            _maxMana = stats.Mana;
            _currentMana = _maxMana;
            _regenPerSecond = stats.ManaRegen;
        }

        public void SetStats(CharacterStats stats)
        {
            float ratio = ManaRatio;

            _maxMana = stats.Mana;
            _currentMana = _maxMana * ratio;
            _regenPerSecond = stats.ManaRegen;

            OnManaChanged?.Invoke(_currentMana, _maxMana);
        }

        public void Tick(float deltaTime)
        {
            if (_regenPerSecond <= 0f || _currentMana >= _maxMana)
                return;

            _currentMana = Mathf.Min(_maxMana, _currentMana + _regenPerSecond * deltaTime);
            OnManaChanged?.Invoke(_currentMana, _maxMana);
        }

        public bool Has(float amount)
        {
            return _currentMana >= amount;
        }

        public bool Spend(float amount)
        {
            if (!Has(amount))
                return false;

            _currentMana -= amount;
            OnManaChanged?.Invoke(_currentMana, _maxMana);
            return true;
        }
    }
}
