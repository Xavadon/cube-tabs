using _Project.Scripts.Architecture.BehaviorTree;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.iOS;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    public class CharacterNodes
    {
    
    }

    public class HasTarget : BTNode
    {
        public override NodeStatus Evaluate()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            
            if (target != null)
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Failure;
        }
    }

    public class IsInRange : BTNode
    {
        private readonly float _range;

        public IsInRange(float range)
        {
            _range = range;
        }
        
        public override NodeStatus Evaluate()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);

            if (target == null)
            {
                return Status = NodeStatus.Failure;
            }
            
            float distance = Vector3.Distance(transform.position, target.position);
            
            if (distance <= _range)
            {
                return Status = NodeStatus.Success;
            }
            
            return Status = NodeStatus.Failure;
        }
    }

    public class FindTarget : BTNode
    {
        private readonly float _radius;
        private readonly LayerMask _layer;

        public FindTarget(float radius, LayerMask layer)
        {
            _radius = radius;
            _layer = layer;
        }

        public override NodeStatus Evaluate()
        {
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            Collider[] colliders = Physics.OverlapSphere(transform.position, _radius, _layer);

            if (colliders.Length > 0)
            {
                Blackboard.Set(BrainKeys.Target, colliders[0].transform);
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Failure;
        }
    }

    public class ChaseTarget : BTNode
    {
        private readonly float _stopDistance;

        public ChaseTarget(float stopDistance)
        {
            _stopDistance = stopDistance;
        }

        public override NodeStatus Evaluate()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            
            agent.SetDestination(target.position);

            if (agent.remainingDistance <= _stopDistance)
            {
                return Status = NodeStatus.Success;
            }
            
            return Status = NodeStatus.Running;
        }
    }

    public class Flee : BTNode
    {
        private readonly float _fleeDistance;

        public Flee(float fleeDistance)
        {
            _fleeDistance = fleeDistance;
        }

        public override NodeStatus Evaluate()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
            {
                return Status = NodeStatus.Failure;
            }
            
            Vector3 direction = (transform.position - target.position).normalized;
            Vector3 fleePoint = transform.position + direction * _fleeDistance;

            if (NavMesh.SamplePosition(fleePoint, out var hit, _fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(fleePoint);
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget >= _fleeDistance)
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
        }
    }

    public class StopMovement : BTNode
    {
        public override NodeStatus Evaluate()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = true;
            return Status = NodeStatus.Success;
        }
    }
    
    public class ResumeMovement : BTNode
    {
        public override NodeStatus Evaluate()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = false;
            return Status = NodeStatus.Success;
        }
    }

    public abstract class AttackBase : BTNode
    {
        private readonly float _windUpDuration;

        private float _elapsed;
        private bool _isAttacking;

        public AttackBase(float windUpDuration)
        {
            _windUpDuration = windUpDuration;
        }

        public override NodeStatus Evaluate()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);

            if (!_isAttacking)
            {
                _isAttacking = true;
                _elapsed = 0;
                agent.isStopped = true;
            }
            
            _elapsed += Time.deltaTime;

            if (_elapsed >= _windUpDuration)
            {
                PerformAttack();
                agent.isStopped = false;
                _isAttacking = false;
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Failure;
        }
        
        public override void Reset()
        {
            base.Reset();
            _isAttacking = false;
            _elapsed = 0;
        }
        
        protected abstract void PerformAttack();
    }

    public class MeleeAttack : AttackBase
    {
        public MeleeAttack(float windUpDuration) : base(windUpDuration)
        {
            
        }
        
        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            Debug.Log("Melee Attack");
        }
    }
    
    public class RangedAttack : AttackBase
    {
        public RangedAttack(float windUpDuration) : base(windUpDuration)
        {
            
        }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            Debug.Log("Range Attack");
        }
    }
}
