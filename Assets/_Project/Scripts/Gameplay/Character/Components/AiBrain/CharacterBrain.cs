using System;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    public static class BrainKeys
    {
        public const string Target = "Target";
        public const string Agent = "Agent";
        public const string AnimatorController = "AnimatorController";
        public const string Transform = "Transform";
        public const string WeaponData = "WeaponData";
        public const string Damage = "Damage";
        public const string DamageType = "DamageType";
        public const string TargetLayer = "TargetLayer";
        public const string InputService = "InputService";
        public const string LockedTarget = "LockedTarget";
        public const string HitPoint = "HitPoint";
        public const string AttackSounds = "AttackSounds";
        public const string OnAttackCallback = "OnAttackCallback";
        public const string AttackIntervalMultiplier = "AttackIntervalMultiplier";
    }
    
    public class CharacterBrain
    {
        private readonly BehaviourTree _tree;

        public Blackboard Blackboard => _tree?.Blackboard;

        public CharacterBrain(LayerMask layerMask, BrainDataBase dataBase, TierData tier, CharacterStats stats,
            NavMeshAgent agent, AnimatorController animatorController, Transform transform, WeaponData weaponData,
            IInputService inputService = null, Action onAttack = null)
        {
            _tree = new BehaviourTree(dataBase.BuildTree(layerMask, tier));
            _tree.Blackboard.Set(BrainKeys.Agent, agent);
            _tree.Blackboard.Set(BrainKeys.Transform, transform);
            _tree.Blackboard.Set(BrainKeys.AnimatorController, animatorController);
            _tree.Blackboard.Set(BrainKeys.TargetLayer, layerMask);
            _tree.Blackboard.Set(BrainKeys.Damage, stats.Damage);
            _tree.Blackboard.Set(BrainKeys.DamageType, stats.DamageType);
            _tree.Blackboard.Set(BrainKeys.AttackIntervalMultiplier, stats.AttackIntervalMultiplier);

            if (weaponData != null)
            {
                _tree.Blackboard.Set(BrainKeys.WeaponData, weaponData);
            }

            if (inputService != null)
            {
                _tree.Blackboard.Set(BrainKeys.InputService, inputService);
            }

            if (tier.AttackSounds != null && tier.AttackSounds.Length > 0)
            {
                _tree.Blackboard.Set(BrainKeys.AttackSounds, tier.AttackSounds);
            }

            if (onAttack != null)
            {
                _tree.Blackboard.Set(BrainKeys.OnAttackCallback, onAttack);
            }
        }

        public void SetStats(CharacterStats stats)
        {
            _tree.Blackboard.Set(BrainKeys.Damage, stats.Damage);
            _tree.Blackboard.Set(BrainKeys.DamageType, stats.DamageType);
            _tree.Blackboard.Set(BrainKeys.AttackIntervalMultiplier, stats.AttackIntervalMultiplier);
        }

        public void Tick()
        {
            _tree?.Tick();
        }
    }
}


/*private void OnDrawGizmosSelected()
        {
            if (_brainData == null) return;

            // Радиус обнаружения
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _brainData.DetectionRadius);

            // Радиус атаки
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _brainData.AttackRange);

            // Линия к цели
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _target.position);
            }
        }*/