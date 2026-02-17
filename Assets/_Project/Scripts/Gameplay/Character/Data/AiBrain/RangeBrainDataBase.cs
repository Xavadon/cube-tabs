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
        [field: SerializeField]
        public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField]
        public float FleeRange { get; private set; } = 3f;
       
        [field: SerializeField] 
        public float WindUpDuration { get; private set; } = 0.3f;

        [field: SerializeField]
        public float AttackRange { get; private set; } = 10f;

        [field: SerializeField] 
        public float AttackCooldown { get; private set; } = 2f;

        [field: SerializeField]
        public LayerMask TargetLayer { get; private set; }

        public override BTNode BuildTree()
        {
            return new Selector(
                new Sequence(
                    new HasTarget(),
                    new IsInRange(FleeRange),
                    new Flee(FleeRange)
                ),
                new Sequence(
                    new HasTarget(),
                    new IsInRange(AttackRange),
                    new StopMovement(),
                    new Cooldown(AttackCooldown, new RangedAttack(WindUpDuration)),
                    new ResumeMovement()
                ),
                new Sequence(
                    new HasTarget(),
                    new ChaseTarget(AttackRange)
                ),
                new FindTarget(DetectionRadius, TargetLayer),
                new Wait(1f)
            );
        }
    }
}