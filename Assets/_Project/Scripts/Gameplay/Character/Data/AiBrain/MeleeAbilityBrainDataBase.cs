using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/MeleeAbilityBrain")]
    public class MeleeAbilityBrainDataBase : MeleeBrainDataBase
    {
        [field: SerializeField] public float AbilityRange { get; private set; } = 15f;

        [field: SerializeField] public float AbilityCooldown { get; private set; } = 5f;

        [field: SerializeField] public float AbilityWindUpDuration { get; private set; } = 0.2f;

        [field: SerializeField] public float AbilityDuration { get; private set; } = 0.5f;

        [field: SerializeField]
        public AbilityAnimationType AbilityAnimation { get; private set; } = AbilityAnimationType.AbilityAttack;

        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Parallel
                (
                    CreateFindTargetNode(targetLayer),
                    new ReactiveSelector
                    (
                        new Sequence
                        (
                            new IsInRange(AbilityRange),
                            new Cooldown(AbilityCooldown,
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

        protected virtual BTNode CreateFindTargetNode(LayerMask targetLayer)
        {
            return FindTargetType switch
            {
                FindTargetType.Priority => new FindPriorityTarget(DetectionRadius, targetLayer),
                _ => new FindTarget(DetectionRadius, targetLayer)
            };
        }

        protected virtual AbilityAttack CreateAbilityNode(TierData tier)
        {
            return new AbilityAttack(tier.Ability, AbilityWindUpDuration, AbilityDuration, AbilityRange,
                AbilityBrainDataBase.ResolveAnimation(AbilityAnimation));
        }
    }
}
