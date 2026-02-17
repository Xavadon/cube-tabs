using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Health.DamageStrategies;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Character.Health.DamageStrategies
{
    //TODO: нужно ли делать как сервис?
    public class DamageStrategyFactory
    {
        private readonly Dictionary<DamageType, IDamageStrategy> _strategies;
        
        public DamageStrategyFactory()
        {
            _strategies = new Dictionary<DamageType, IDamageStrategy>
            {
                { DamageType.Physical, new PhysicalDamageStrategy() },
                { DamageType.Magic, new MagicDamageStrategy() },
                { DamageType.Fire, new FireDamageStrategy() },
                { DamageType.Faith, new FaithDamageStrategy() }
            };

            Debug.Log($"[DamageStrategyFactory] Зарегистрировано {_strategies.Count} стратегий урона");
        }
        
        public IDamageStrategy GetStrategy(DamageType damageType)
        {
            if (_strategies.TryGetValue(damageType, out var strategy))
            {
                return strategy;
            }

            throw new ArgumentException(
                $"[DamageStrategyFactory] Стратегия для типа урона '{damageType}' не зарегистрирована! " +
                $"Добавьте стратегию в конструктор DamageStrategyFactory.",
                nameof(damageType));
        }

        public bool HasStrategy(DamageType damageType)
        {
            return _strategies.ContainsKey(damageType);
        }

        public IEnumerable<DamageType> GetRegisteredDamageTypes()
        {
            return _strategies.Keys;
        }

        public void DebugPrintStrategies()
        {
            Debug.Log($"[DamageStrategyFactory] === Зарегистрированные стратегии урона ===");
            foreach (var kvp in _strategies)
            {
                Debug.Log($"  {kvp.Key} -> {kvp.Value.GetType().Name} ({kvp.Value.GetDamageTypeName()})");
            }
        }
    }
}
