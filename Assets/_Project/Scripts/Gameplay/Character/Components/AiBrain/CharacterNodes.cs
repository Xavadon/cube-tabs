using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    public class CharacterNodes
    {
    
    }

    public class HasTarget : BTNode
    {
        protected override NodeStatus Process()
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
        
        protected override NodeStatus Process()
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

        protected override NodeStatus Process()
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

        protected override void Enter()
        {
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.Animator);
            animator.PlayMoveAnimation();
        }

        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.stoppingDistance = _stopDistance;

            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            agent.SetDestination(target.position);

            if (agent.remainingDistance <= _stopDistance)
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
        }

        protected override void Exit()
        {
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.Animator);
            animator.PlayIdleAnimation();
        }
    }

    public class Flee : BTNode
    {
        private readonly float _fleeDistance;

        public Flee(float fleeDistance)
        {
            _fleeDistance = fleeDistance;
        }

        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.stoppingDistance = 1;
            
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
            {
                return Status = NodeStatus.Failure;
            }

            agent.isStopped = false;

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
        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = true;
            return Status = NodeStatus.Success;
        }
    }
    
    public class ResumeMovement : BTNode
    {
        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = false;
            return Status = NodeStatus.Success;
        }
    }

    public class LookAtTarget : BTNode
    {
        private readonly float _rotationSpeed;

        public LookAtTarget(float rotationSpeed = 10f)
        {
            _rotationSpeed = rotationSpeed;
        }

        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
                return Status = NodeStatus.Failure;

            agent.updateRotation = false;

            Vector3 direction = target.position - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }

            return Status = NodeStatus.Success;
        }

        public override void Reset()
        {
            base.Reset();
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            if (agent != null)
                agent.updateRotation = true;
        }
    }

    public abstract class AttackBase : BTNode
    {
        private readonly float _windUpDuration;
        private readonly float _stoppingDistance;

        private float _elapsed;
        private bool _isAttacking;

        public AttackBase(float windUpDuration, float stoppingDistance)
        {
            _windUpDuration = windUpDuration;
            _stoppingDistance = stoppingDistance;
        }

        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.stoppingDistance = _stoppingDistance;
            
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (!_isAttacking)
            {
                _isAttacking = true;
                _elapsed = 0;
                agent.isStopped = true;
                agent.updateRotation = false;
            }

            if (target != null)
            {
                Vector3 direction = target.position - transform.position;
                direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
                }
            }

            _elapsed += Time.deltaTime;

            if (_elapsed >= _windUpDuration)
            {
                PerformAttack();
                agent.isStopped = false;
                agent.updateRotation = true;
                _isAttacking = false;
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
        }
        
        public override void Reset()
        {
            base.Reset();
            _isAttacking = false;
            _elapsed = 0;
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            if (agent != null)
            {
                agent.isStopped = false;
                agent.updateRotation = true;
            }
        }
        
        protected abstract void PerformAttack();
    }

    public class MeleeAttack : AttackBase
    {
        public MeleeAttack(float windUpDuration, float stoppingDistance) : base(windUpDuration, stoppingDistance) { }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            if (target == null) return;

            IDamageAble damageable = target.GetComponent<IDamageAble>();
            if (damageable == null) return;

            WeaponData weapon = Blackboard.Get<WeaponData>(BrainKeys.WeaponData);
            float damage = weapon != null ? weapon.Damage : 10f;
            DamageType type = weapon != null ? weapon.DamageType : DamageType.Physical;

            damageable.ApplyDamage(damage, target.position, type);
        }
    }

    public class RangedAttack : AttackBase
    {
        public RangedAttack(float windUpDuration, float stoppingDistance) : base(windUpDuration, stoppingDistance) { }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            if (target == null) return;

            IDamageAble damageable = target.GetComponent<IDamageAble>();
            if (damageable == null) return;

            WeaponData weapon = Blackboard.Get<WeaponData>(BrainKeys.WeaponData);
            float damage = weapon != null ? weapon.Damage : 10f;
            DamageType type = weapon != null ? weapon.DamageType : DamageType.Physical;

            damageable.ApplyDamage(damage, target.position, type);
        }
    }
}
