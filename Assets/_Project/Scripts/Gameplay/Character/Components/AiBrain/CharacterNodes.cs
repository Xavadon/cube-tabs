using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.State_Machine;
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

        protected override void Enter()
        {
            base.Enter();
            
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.AnimatorController);
            animator.PlayIdle();
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

    public class RotateTowardsTarget : BTNode
    {
        private readonly float _threshold;
        private readonly float _rotationSpeed;

        public RotateTowardsTarget(float threshold = 5f, float rotationSpeed = 360f)
        {
            _threshold = threshold;
            _rotationSpeed = rotationSpeed;
        }

        protected override void Enter()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.updateRotation = false;
        }

        protected override void Exit()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.updateRotation = true;
        }

        protected override NodeStatus Process()
        {
            Transform self = Blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (self == null || target == null)
            {
                return Status = NodeStatus.Failure;
            }

            Vector3 direction = target.position - self.position;
            direction.y = 0f;

            if (direction == Vector3.zero)
            {
                return Status = NodeStatus.Success;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            self.rotation = Quaternion.RotateTowards(self.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

            float angle = Quaternion.Angle(self.rotation, targetRotation);
            
            if (angle < _threshold)
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
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
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.AnimatorController);
            animator.PlayMove();
        }

        protected override NodeStatus Process()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.stoppingDistance = _stopDistance;

            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
            {
                return Status = NodeStatus.Failure;
            }
            
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

        protected override void Enter()
        {
            base.Enter();
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.AnimatorController);
            animator.PlayMove();
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

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget >= _fleeDistance)
            {
                return Status = NodeStatus.Success;
            }

            agent.isStopped = false;

            Vector3 direction = (transform.position - target.position).normalized;

            if (!TryFindFleePoint(transform.position, direction, out Vector3 destination))
            {
                return Status = NodeStatus.Failure;
            }

            agent.SetDestination(destination);
            return Status = NodeStatus.Running;
        }

        private bool TryFindFleePoint(Vector3 origin, Vector3 direction, out Vector3 result)
        {
            float sampleRadius = _fleeDistance * 0.5f;

            // Try direct, then +-45, +-90, +-135, 180
            float[] angles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };

            foreach (float angle in angles)
            {
                Vector3 rotated = Quaternion.Euler(0f, angle, 0f) * direction;
                Vector3 candidate = origin + rotated * _fleeDistance;

                if (NavMesh.SamplePosition(candidate, out var hit, sampleRadius, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = default;
            return false;
        }
    }
    
    public abstract class AttackBase : BTNode
    {
        private readonly float _windUpDuration;
        private readonly float _attackDuration;
        private readonly float _stoppingDistance;

        private float _elapsedTime;
        private bool _isAttacking;

        public AttackBase(float windUpDuration, float attackDuration, float stoppingDistance)
        {
            _windUpDuration = windUpDuration;
            _attackDuration = attackDuration;
            _stoppingDistance = stoppingDistance;
        }

        protected override void Enter()
        {
            base.Enter();
            
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.stoppingDistance = _stoppingDistance;
            
            _elapsedTime = 0f;
        }

        protected override void Exit()
        {
            base.Exit();
            _isAttacking = false;
        }

        protected override NodeStatus Process()
        {
            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= _windUpDuration && !_isAttacking)
            {
                _isAttacking = true;
                PerformAttack();
            }

            if (_elapsedTime >= _attackDuration)
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
        }
        
        protected abstract void PerformAttack();
    }

    public class MeleeAttack : AttackBase
    {
        public MeleeAttack(float windUpDuration, float attackDuration, float stoppingDistance) : base(windUpDuration, attackDuration, stoppingDistance)
        {
            
        }

        protected override void Enter()
        {
            base.Enter();
            
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.AnimatorController);
            animator.PlayAttack();
        }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            
            if (target == null)
            {
                return;
            }

            IDamageAble damageable = target.GetComponent<IDamageAble>();
            
            if (damageable == null)
            {
                return;
            }

            WeaponData weapon = Blackboard.Get<WeaponData>(BrainKeys.WeaponData);
            float damage = 10f;
            
            if (weapon != null)
            {
                damage = weapon.Damage;
            }
            
            DamageType type = DamageType.Physical;
            
            if (weapon != null)
            {
                type = weapon.DamageType;
            }

            damageable.ApplyDamage(damage, target.position, type);
        }
    }

    public class RangedAttack : AttackBase
    {
        public RangedAttack(float windUpDuration, float attackDuration, float stoppingDistance) : base(windUpDuration, attackDuration, stoppingDistance)
        {
            
        }

        protected override void Enter()
        {
            base.Enter();
            
            AnimatorConroller animator = Blackboard.Get<AnimatorConroller>(BrainKeys.AnimatorController);
            animator.PlayRangeAttack();
        }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            
            if (target == null)
            {
                return;
            }

            IDamageAble damageable = target.GetComponent<IDamageAble>();
            
            if (damageable == null)
            {
                return;
            }

            WeaponData weapon = Blackboard.Get<WeaponData>(BrainKeys.WeaponData);
            float damage = 10f;
            
            if (weapon != null)
            {
                damage = weapon.Damage;
            }
            
            DamageType type = DamageType.Physical;;
            
            if (weapon != null)
            {
                type = weapon.DamageType;
            }

            damageable.ApplyDamage(damage, target.position, type);
        }
    }
}
