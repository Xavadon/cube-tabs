using System;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Architecture.State_Machine;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    public class CharacterNodes
    {

    }

    public class Idle : BTNode
    {
        protected override void Enter()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = true;

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            animator.PlayIdle();
        }

        protected override void Exit()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = false;
        }

        protected override NodeStatus Process()
        {
            return Status = NodeStatus.Success;
        }
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
        private const int MaxHits = 32;

        private readonly float _radius;
        private readonly LayerMask _layer;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];

        public FindTarget(float radius, LayerMask layer)
        {
            _radius = radius;
            _layer = layer;
        }

        public override NodeStatus Evaluate()
        {
            UpdateTarget();
            return base.Evaluate();
        }

        protected override NodeStatus Process()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
            {
                return Status = NodeStatus.Failure;
            }

            return Status = NodeStatus.Success;
        }

        private void UpdateTarget()
        {
            Transform transform = Blackboard.Get<Transform>(BrainKeys.Transform);
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hitBuffer, _layer);

            if (hitCount == 0)
            {
                Blackboard.Set<Transform>(BrainKeys.Target, null);
                return;
            }

            Transform closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                float dist = Vector3.Distance(transform.position, _hitBuffer[i].transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = _hitBuffer[i].transform;
                }
            }

            Blackboard.Set(BrainKeys.Target, closest);
        }
    }

    public class FindPriorityTarget : BTNode
    {
        private const int MaxHits = 32;

        private readonly float _radius;
        private readonly LayerMask _layer;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];

        public FindPriorityTarget(float radius, LayerMask layer)
        {
            _radius = radius;
            _layer = layer;
        }

        public override NodeStatus Evaluate()
        {
            UpdateTarget();
            return base.Evaluate();
        }

        protected override NodeStatus Process()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);
            return Status = target != null ? NodeStatus.Success : NodeStatus.Failure;
        }

        private void UpdateTarget()
        {
            Transform self = Blackboard.Get<Transform>(BrainKeys.Transform);
            int hitCount = Physics.OverlapSphereNonAlloc(self.position, _radius, _hitBuffer, _layer);

            if (hitCount == 0)
            {
                Blackboard.Set<Transform>(BrainKeys.Target, null);
                return;
            }

            Transform bestTarget = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Transform candidate = _hitBuffer[i].transform;
                float dist = Vector3.Distance(self.position, candidate.position);

                bool isRanged = candidate.TryGetComponent(out Character character) && character.IsRanged;

                // Ranged targets get priority: scored lower so they sort first
                float score = isRanged ? dist : dist + 10000f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            Blackboard.Set(BrainKeys.Target, bestTarget);
        }
    }

    public class FindWeakestAlly : BTNode
    {
        private const int MaxHits = 32;

        private readonly float _radius;
        private readonly LayerMask _allyLayer;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];

        public FindWeakestAlly(float radius, LayerMask allyLayer)
        {
            _radius = radius;
            _allyLayer = allyLayer;
        }

        public override NodeStatus Evaluate()
        {
            UpdateTarget();
            return base.Evaluate();
        }

        protected override NodeStatus Process()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
                return Status = NodeStatus.Failure;

            return Status = NodeStatus.Success;
        }

        private void UpdateTarget()
        {
            Transform self = Blackboard.Get<Transform>(BrainKeys.Transform);

            int hitCount = Physics.OverlapSphereNonAlloc(self.position, _radius, _hitBuffer, _allyLayer);

            if (hitCount == 0)
            {
                Blackboard.Set<Transform>(BrainKeys.Target, null);
                return;
            }

            Transform weakest = null;
            float lowestRatio = 1f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];

                // Пропускаем себя
                if (hit.transform == self)
                    continue;

                if (!hit.TryGetComponent(out IHealable healable))
                    continue;

                // Пропускаем союзников с полным HP
                if (healable.HealthRatio >= 1f)
                    continue;

                if (healable.HealthRatio < lowestRatio)
                {
                    lowestRatio = healable.HealthRatio;
                    weakest = hit.transform;
                }
            }

            Blackboard.Set(BrainKeys.Target, weakest);
        }
    }

    public class FindClosestAlly : BTNode
    {
        private const int MaxHits = 32;

        private readonly float _radius;
        private readonly LayerMask _allyLayer;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];

        public FindClosestAlly(float radius, LayerMask allyLayer)
        {
            _radius = radius;
            _allyLayer = allyLayer;
        }

        public override NodeStatus Evaluate()
        {
            UpdateTarget();
            return base.Evaluate();
        }

        protected override NodeStatus Process()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
                return Status = NodeStatus.Failure;

            return Status = NodeStatus.Success;
        }

        private void UpdateTarget()
        {
            Transform self = Blackboard.Get<Transform>(BrainKeys.Transform);

            int hitCount = Physics.OverlapSphereNonAlloc(self.position, _radius, _hitBuffer, _allyLayer);

            Transform closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                if (_hitBuffer[i].transform == self)
                    continue;

                float dist = Vector3.Distance(self.position, _hitBuffer[i].transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = _hitBuffer[i].transform;
                }
            }

            Blackboard.Set(BrainKeys.Target, closest);
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
            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
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

        protected override void Exit()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = true;
            agent.isStopped = false;
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
            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
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
        private bool _isWindingUp;
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
            _isWindingUp = true;
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
        private readonly Action<AnimatorController> _playAnimation;

        public MeleeAttack(float windUpDuration, float attackDuration, float stoppingDistance,
            MeleeAnimationType animationType = MeleeAnimationType.OneHanded)
            : base(windUpDuration, attackDuration, stoppingDistance)
        {
            _playAnimation = ResolveAnimation(animationType);
        }

        protected override void Enter()
        {
            base.Enter();

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            _playAnimation(animator);
        }

        public static Action<AnimatorController> ResolveAnimation(MeleeAnimationType type) => type switch
        {
            MeleeAnimationType.TwoHanded => a => a.PlayTwoHanded(),
            MeleeAnimationType.Dual => a => a.PlayDual(),
            MeleeAnimationType.Spear => a => a.PlaySpear(),
            _ => a => a.PlayOneHanded()
        };

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
                return;

            IDamageAble damageable = target.GetComponent<IDamageAble>();

            if (damageable == null)
                return;

            float damage = Blackboard.Get<float>(BrainKeys.Damage);
            DamageType type = Blackboard.Get<DamageType>(BrainKeys.DamageType);

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

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            animator.PlayRangeAttack();
        }

        protected override void PerformAttack()
        {
            Transform target = Blackboard.Get<Transform>(BrainKeys.Target);

            if (target == null)
                return;

            IDamageAble damageable = target.GetComponent<IDamageAble>();

            if (damageable == null)
                return;

            float damage = Blackboard.Get<float>(BrainKeys.Damage);
            DamageType type = Blackboard.Get<DamageType>(BrainKeys.DamageType);

            damageable.ApplyDamage(damage, target.position, type);
        }
    }

    public class AbilityAttack : AttackBase
    {
        private readonly AbilityDataBase _abilityDataBase;
        private readonly Action<AnimatorController> _playAnimation;

        public AbilityAttack(AbilityDataBase abilityDataBase, float windUpDuration, float attackDuration,
            float stoppingDistance, Action<AnimatorController> playAnimation)
            : base(windUpDuration, attackDuration, stoppingDistance)
        {
            _abilityDataBase = abilityDataBase;
            _playAnimation = playAnimation;
        }

        protected override void Enter()
        {
            base.Enter();

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            _playAnimation(animator);
        }

        protected override void PerformAttack()
        {
            float damage = Blackboard.Get<float>(BrainKeys.Damage);
            DamageType type = Blackboard.Get<DamageType>(BrainKeys.DamageType);
            _abilityDataBase.Execute(Blackboard, damage, type);
        }
    }

    public class HasMoveInput : BTNode
    {
        private const float Deadzone = 0.1f;

        protected override NodeStatus Process()
        {
            IInputService input = Blackboard.Get<IInputService>(BrainKeys.InputService);
            
            if (input.MoveInput.sqrMagnitude > Deadzone * Deadzone)
            {
                return Status = NodeStatus.Success;
            }
            else
            {
                return Status = NodeStatus.Failure;
            }
        }
    }

    public class PlayerMove : BTNode
    {
        private readonly float _moveSpeed;
        private readonly float _destinationStep;

        public PlayerMove(float moveSpeed, float destinationStep = 2f)
        {
            _moveSpeed = moveSpeed;
            _destinationStep = destinationStep;
        }

        protected override void Enter()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.speed = _moveSpeed;
            agent.stoppingDistance = 0.1f;

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            animator.PlayMove();
        }

        protected override NodeStatus Process()
        {
            IInputService input = Blackboard.Get<IInputService>(BrainKeys.InputService);
            Vector2 raw = input.MoveInput;

            if (raw.sqrMagnitude < 0.01f)
                return Status = NodeStatus.Success;

            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            Transform self = Blackboard.Get<Transform>(BrainKeys.Transform);

            Vector3 direction = new Vector3(raw.x, 0f, raw.y).normalized;
            Vector3 destination = self.position + direction * _destinationStep;

            agent.SetDestination(destination);
            self.rotation = Quaternion.LookRotation(direction);

            Blackboard.Set<Transform>(BrainKeys.Target, null);

            return Status = NodeStatus.Running;
        }

        protected override void Exit()
        {
            NavMeshAgent agent = Blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            agent.isStopped = true;
            agent.isStopped = false;

            AnimatorController animator = Blackboard.Get<AnimatorController>(BrainKeys.AnimatorController);
            animator.PlayIdle();
        }
    }
    
    public class LockWhileRunning : BTNode
    {
        private readonly BTNode _child;
        private bool _isLocked;

        public LockWhileRunning(BTNode child)
        {
            _child = child;
        }

        protected override NodeStatus Process()
        {
            NodeStatus status = _child.Evaluate();

            if (status == NodeStatus.Running)
            {
                _isLocked = true;
                return Status = NodeStatus.Locked;
            }

            if (_isLocked)
            {
                _isLocked = false;
                return Status = status;
            }

            return Status = status;
        }

        public override void Reset()
        {
            base.Reset();
            _isLocked = false;
            _child.Reset();
        }

        protected override void OnBlackboardSet()
        {
            _child.SetBlackboard(Blackboard);
        }
    }
    
    public class LockTarget : BTNode
    {
        private readonly string _sourceKey;
        private readonly string _targetKey;

        public LockTarget(string sourceKey, string targetKey)
        {
            _sourceKey = sourceKey;
            _targetKey = targetKey;
        }

        protected override NodeStatus Process()
        {
            Transform target = Blackboard.Get<Transform>(_sourceKey);

            if (target == null)
                return Status = NodeStatus.Failure;

            Blackboard.Set(_targetKey, target);
            return Status = NodeStatus.Success;
        }
    }
}
