using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/HealerBrain")]
    public class HealerBrainDataBase : BrainDataBase
    {
        [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField] public float FleeRange { get; private set; } = 5f;

        [field: SerializeField] public float WindUpDuration { get; private set; } = 0.3f;

        [field: SerializeField] public float AttackDuration { get; private set; } = 2f;

        [field: SerializeField] public float HealRange { get; private set; } = 8f;

        [field: SerializeField] public float HealCooldown { get; private set; } = 3f;

        [field: SerializeField] public float FollowDistance { get; private set; } = 4f;

        [field: SerializeField] public LayerMask AllyLayer { get; private set; }

        [field: SerializeField]
        public AbilityAnimationType AbilityAnimation { get; private set; } = AbilityAnimationType.AbilityAttack;

        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                /*// 1. Враг близко → убегать
                new Sequence
                (
                    new FindTarget(FleeRange, targetLayer),
                    new Flee(FleeRange)
                ),*/
                // 2. Есть раненый союзник → хилить
                new Parallel
                (
                    new FindWeakestAlly(DetectionRadius, AllyLayer),
                    new Selector
                    (
                        new Sequence
                        (
                            new IsInRange(HealRange),
                            new Selector
                            (
                                new Cooldown(HealCooldown,
                                    new Parallel(
                                        new AbilityAttack(tier.Ability, WindUpDuration, AttackDuration, HealRange,
                                            AbilityBrainDataBase.ResolveAnimation(AbilityAnimation)),
                                        new RotateTowardsTarget()
                                    )
                                ),
                                new Wait(0.1f)
                            )
                        ),
                        new ChaseTarget(HealRange)
                    )
                ),
                // 3. Все здоровы → следовать за ближайшим союзником
                new Parallel
                (
                    new FindClosestAlly(DetectionRadius, AllyLayer),
                    new ChaseTarget(FollowDistance)
                ),
                // 4. Никого нет
                new Idle()
            );
        }
    }
}
