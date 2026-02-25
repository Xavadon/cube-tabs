using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/AbilityBrain")]
    public class AbilityBrainDataBase : BrainDataBase
    {
        [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField] public float FleeRange { get; private set; } = 5f;

        [field: SerializeField] public float WindUpDuration { get; private set; } = 0.3f;

        [field: SerializeField] public float AttackDuration { get; private set; } = 2f;

        [field: SerializeField] public float AttackRange { get; private set; } = 10f;

        [field: SerializeField] public float AttackCooldown { get; private set; } = 2f;
        
        [field: SerializeField] public AbilityDataBase Ability { get; private set; } 
        
        public override BTNode BuildTree(LayerMask targetLayer)
        {
            return new Selector(
                new Sequence(
                    new FindTarget(DetectionRadius, targetLayer),
                    new Selector(
                        BuildFleeSequence(),
                        BuildAttackSequence(),
                        BuildChaseSequence()
                    )
                ),
                new Idle()
            );
        }

        private BTNode BuildFleeSequence()
        {
            return new Sequence(
                new IsInRange(FleeRange),
                new Flee(FleeRange)
            );
        }

        private BTNode BuildAttackSequence()
        {
            return new Sequence(
                new IsInRange(AttackRange),
                new Selector(
                    new Cooldown(AttackCooldown,
                        new Parallel(
                            new AbilityAttack(Ability, WindUpDuration, AttackDuration, AttackRange),
                            new RotateTowardsTarget()
                        )
                    ),
                    new Wait(0.1f)
                )
            );
        }

        private BTNode BuildChaseSequence()
        {
            return new ChaseTarget(AttackRange);
        }
    }
}