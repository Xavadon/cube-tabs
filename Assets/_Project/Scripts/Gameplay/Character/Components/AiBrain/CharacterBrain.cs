using _Project.Scripts.Architecture.BehaviorTree;
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
        public const string Transform = "Transform";
        public const string WeaponData = "WeaponData";
    }
    
    public class CharacterBrain
    {
        private readonly BehaviourTree _tree;

        public CharacterBrain(BrainDataBase dataBase, NavMeshAgent agent, Transform transform, WeaponData weaponData)
        {
            _tree = new BehaviourTree(dataBase.BuildTree());
            _tree.Blackboard.Set(BrainKeys.Agent, agent);
            _tree.Blackboard.Set(BrainKeys.Transform, transform);
            if (weaponData != null)
                _tree.Blackboard.Set(BrainKeys.WeaponData, weaponData);
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