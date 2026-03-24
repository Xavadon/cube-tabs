using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/RogueBrain")]
    public class RogueBrainDataBase : MeleeBrainDataBase
    {
        [field: SerializeField] public float BlinkRange { get; private set; } = 15f;

        [field: SerializeField] public float BlinkCooldown { get; private set; } = 5f;

        [field: SerializeField] public float BlinkWindUpDuration { get; private set; } = 0.2f;

        [field: SerializeField] public float BlinkDuration { get; private set; } = 0.5f;

        [field: SerializeField]
        public AbilityAnimationType BlinkAnimation { get; private set; } = AbilityAnimationType.AbilityAttack;

        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Parallel
                (
                    new FindPriorityTarget(DetectionRadius, targetLayer),
                    new ReactiveSelector
                    (
                        new Sequence
                        (
                            new IsInRange(BlinkRange),
                            new Cooldown(BlinkCooldown,
                                new Parallel(CreateAbilityNode(tier), new RotateTowardsTarget()))
                        ),
                        new Sequence
                        (
                            new IsInRange(AttackStopRange),
                            new Selector
                            (
                                new Cooldown(AttackCooldown,
                                    new Parallel(CreateAttackNode(), new RotateTowardsTarget())),
                                new Wait(0.1f)
                            )
                        ),
                        new ChaseTarget(AttackRange)
                    )
                ),
                new Idle()
            );
        }

        public AbilityAttack CreateAbilityNode(TierData tier)
        {
            return new AbilityAttack(tier.Ability, BlinkWindUpDuration, BlinkDuration, BlinkRange,
                AbilityBrainDataBase.ResolveAnimation(BlinkAnimation));
        }
    }
}