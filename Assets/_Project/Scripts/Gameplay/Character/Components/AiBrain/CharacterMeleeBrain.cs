using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    /// <summary>
    /// Пример использования BehaviourTree для врага
    /// </summary>
    public class CharacterMeleeBrain : MonoBehaviour
    {
        [SerializeField] private LayerMask _playerLayer;
        
        private float _detectionRadius;
        private float _attackRange;
        private float _attackCooldown;

        private BehaviourTree _tree;
        private NavMeshAgent _agent;
        private Transform _target;
        
        public void Initialize(NavMeshAgent agent, AiBrainData data)
        {
            _agent = agent;
            _detectionRadius = data.DetectionRadius;
            _attackRange = data.AttackRange;
            _attackCooldown = data.AttackCooldown;
            
            _tree = new BehaviourTree(BuildTree());
        }

        private void Update()
        {
            if (_tree == null)
            {
                return;
            }
            
            _tree.Tick();
        }

        private BTNode BuildTree()
        {
            return new Selector(
                // Ветка 1: Атака
                new Sequence
                (
                    new Condition(HasTarget),
                    new Condition(IsInAttackRange),
                    new Cooldown(_attackCooldown, new Action(Attack))
                ),

                // Ветка 2: Преследование
                new Sequence
                (
                    new Condition(HasTarget),
                    new Action(ChaseTarget)
                ),

                // Ветка 3: Поиск цели
                new Action(FindTarget),

                // Ветка 4: Idle
                new Wait(1f)
            );
        }

        //========== CONDITIONS ==========
        private bool HasTarget()
        {
            return _target != null;
        }

        private bool IsInAttackRange()
        {
            return _target != null && Vector3.Distance(transform.position, _target.position) <= _attackRange;
        }


        //========== ACTIONS ==========
        private NodeStatus FindTarget()
        {
            var colliders = Physics.OverlapSphere(transform.position, _detectionRadius, _playerLayer);

            Debug.Log($"FindTarget: Found {colliders.Length} colliders. LayerMask value: {_playerLayer.value}");

            if (colliders.Length > 0)
            {
                _target = colliders[0].transform;
                Debug.Log($"Target found: {_target.name}");
                return NodeStatus.Success;
            }

            return NodeStatus.Failure;
        }

        private NodeStatus ChaseTarget()
        {
            _agent.SetDestination(_target.position);

            if (_agent.remainingDistance <= _attackRange)
            {
                return NodeStatus.Success;
            }

            return NodeStatus.Running;
        }

        private NodeStatus Attack()
        {
            // Твоя логика атаки
            Debug.Log("Attack!");
            return NodeStatus.Success;
        }

        private void OnDrawGizmosSelected()
        {
            // Радиус обнаружения
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Радиус атаки
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // Линия к цели
            if (_target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _target.position);
            }
        }
    }
}