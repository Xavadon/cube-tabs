using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/RangeBrain")]
    public class RangeBrainDataBase : BrainDataBase
    {
        [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField] public float FleeRange { get; private set; } = 5f;

        [field: SerializeField] public float WindUpDuration { get; private set; } = 0.3f;

        [field: SerializeField] public float AttackDuration { get; private set; } = 2f;

        [field: SerializeField] public float AttackRange { get; private set; } = 10f;

        [field: SerializeField] public float AttackCooldown { get; private set; } = 2f;

        [field: SerializeField] public LayerMask TargetLayer { get; private set; }

        public override BTNode BuildTree()
        {
            return new Selector(
                BuildFleeSequence(),
                BuildAttackSequence(),
                BuildChaseSequence(),
                new FindTarget(DetectionRadius, TargetLayer),
                new Wait(0.1f)
            );
        }

        private BTNode BuildFleeSequence()
        {
            return new Sequence(
                new HasTarget(),
                new IsInRange(FleeRange),
                new Flee(FleeRange)
            );
        }

        private BTNode BuildAttackSequence()
        {
            return new Sequence(
                new HasTarget(),
                new IsInRange(AttackRange),
                new Selector(
                    new Cooldown(AttackCooldown,
                        new Parallel(
                            new RangedAttack(WindUpDuration, AttackDuration, AttackRange),
                            new RotateTowardsTarget()
                        )
                    ),
                    new Wait(0.1f)
                )
            );
        }

        private BTNode BuildChaseSequence()
        {
            return new Sequence(
                new HasTarget(),
                new ChaseTarget(AttackRange)
            );
        }
    }
}