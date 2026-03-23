using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/AssassinBrain")]
    public class AssassinBrainDataBase : MeleeBrainDataBase
    {
        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Parallel
                (
                    new FindPriorityTarget(DetectionRadius, targetLayer),
                    new Selector
                    (
                        new Sequence
                        (
                            new IsInRange(AttackStopRange),
                            new Selector
                            (
                                new Cooldown(AttackCooldown, new Parallel(CreateAttackNode(), new RotateTowardsTarget())),
                                new Wait(0.1f)
                            )
                        ),
                        new ChaseTarget(AttackRange)
                    )
                ),
                new Idle()
            );
        }
    }
}
