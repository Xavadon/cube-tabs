using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    /// <summary>
    /// Пример использования BehaviourTree для врага
    /// </summary>
    public class CharacterMeleeBrain : MonoBehaviour
    {
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private LayerMask _playerLayer;

        private BehaviourTree _tree;
        private NavMeshAgent _agent;
        private Transform _target;
        
        public void Initialize(NavMeshAgent agent)
        {
            _agent = agent;
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

            if (colliders.Length > 0)
            {
                _target = colliders[0].transform;
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
    }
}