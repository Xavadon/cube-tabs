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
        
        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Parallel
                (
                    new FindTarget(DetectionRadius, targetLayer),
                    new Selector
                    (
                        new Sequence
                        (
                            new IsInRange(FleeRange),
                            new Flee(FleeRange)
                        ),
                        new Sequence
                        (
                            new IsInRange(AttackRange),
                            new Selector
                            (
                                new AttackCooldown(AttackCooldown, new Parallel(CreateAttackNode(tier), new RotateTowardsTarget())),
                                new Wait(0.1f)
                            )
                        ),
                        new ChaseTarget(AttackRange)
                    )
                ),
                new Idle()
            );
        }

        protected virtual BTNode CreateAttackNode(TierData tier)
        {
            return new RangedAttack(WindUpDuration, AttackDuration, AttackRange, DetectionRadius);
        }
    }
}