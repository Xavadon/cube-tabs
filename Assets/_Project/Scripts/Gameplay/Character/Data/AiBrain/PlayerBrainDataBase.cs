using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/PlayerBrain")]
    public class PlayerBrainDataBase : BrainDataBase
    {
        [field: SerializeField] public float DetectionRadius { get; private set; } = 5f;

        [field: SerializeField] public float WindUpDuration { get; private set; } = 0.3f;

        [field: SerializeField] public float AttackDuration { get; private set; } = 1f;

        [field: SerializeField] public float AttackRange { get; private set; } = 1.75f;

        [field: SerializeField] public float AttackStopRange { get; private set; } = 3f;

        [field: SerializeField] public float AttackCooldown { get; private set; } = 0.5f;

        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Sequence
                (
                    new HasMoveInput(),
                    new PlayerMove(tier.MoveSpeed)
                ),
                new Sequence
                (
                    new FindTarget(DetectionRadius, targetLayer),
                    new IsInRange(AttackStopRange),
                    new Selector
                    (
                        new Cooldown(AttackCooldown, new Parallel(CreateAttackNode(tier), new RotateTowardsTarget())),
                        new Wait(0.1f)
                    )
                ),
                new Idle()
            );
        }

        protected virtual BTNode CreateAttackNode(TierData tier)
        {
            return new MeleeAttack(WindUpDuration, AttackDuration, AttackRange);
        }
    }
}
