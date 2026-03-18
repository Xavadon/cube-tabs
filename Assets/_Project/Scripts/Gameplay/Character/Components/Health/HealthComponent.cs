using System;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;
using CharacterResistances = _Project.Scripts.Gameplay.Character.Health.DamageStrategies.CharacterResistances;

namespace _Project.Scripts.Gameplay.Character.Components.Health
{
    public interface IDamageAble
    {
        void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical);
    }

    public interface IHealable
    {
        float HealthRatio { get; }
        void Heal(float amount);
    }

    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Faith
    }

    public class HealthComponent
    {
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _maxHealth > 0f ? _currentHealth / _maxHealth : 0f;
        public bool IsAlive => _currentHealth > 0;

        private readonly CharacterResistances _resistances;
        private readonly DamageStrategyFactory _damageStrategyFactory;
        private readonly float _maxHealth;

        private float _currentHealth;
        private string _charaName;  // Debug (TODO: удалить в релизе)

        public event Action OnDeath;
        public event Action<float, float> OnHealthChanged;

        public HealthComponent(TierData tier)
        {
            _damageStrategyFactory = new();
            _maxHealth = tier.Stats.Health;
            _currentHealth = _maxHealth;
            _resistances = CharacterResistances.FromTierData(tier);
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[CharacterHealth] Попытка нанести отрицательный урон: {amount}. Игнорируем.");
                return;
            }

            if (!IsAlive)
            {
                Debug.LogWarning($"[CharacterHealth] Попытка нанести урон мертвому персонажу. Игнорируем.");
                return;
            }

            IDamageStrategy damageStrategy = _damageStrategyFactory.GetStrategy(type);
            float calculatedDamage = damageStrategy.CalculateDamage(amount, _resistances);

            _currentHealth -= calculatedDamage;

            Debug.Log($"[CharacterHealth] {_charaName} получил {calculatedDamage:F1} урона ({damageStrategy.GetDamageTypeName()}) " +
                     $"в точке {hitPoint}. HP: {_currentHealth:F1}/{_maxHealth} " +
                     $"(базовый урон: {amount:F1})");

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                Die();
                return;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        public void Heal(float amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[CharacterHealth] Попытка вылечить на отрицательное значение: {amount}");
                return;
            }

            if (!IsAlive)
            {
                Debug.LogWarning($"[CharacterHealth] Попытка вылечить мертвого персонажа. Используйте Revive().");
                return;
            }

            float healedAmount = Mathf.Min(amount, _maxHealth - _currentHealth);
            _currentHealth += healedAmount;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }

        public void DebugPrintStatus(string charaName)
        {
            _charaName = charaName;

            Debug.Log($"[CharacterHealth] === Статус {_charaName} ===" +
                     $"\n  HP: {_currentHealth:F1}/{_maxHealth}" +
                     $"\n  Жив: {IsAlive}" +
                     $"\n  Физ. сопротивление: {_resistances.PhysicalResist:P0}" +
                     $"\n  Маг. сопротивление: {_resistances.MagicResist:P0}" +
                     $"\n  Огн. сопротивление: {_resistances.FireResist:P0}" +
                     $"\n  Свят. сопротивление: {_resistances.FaithResist:P0}");
        }
    }
}
