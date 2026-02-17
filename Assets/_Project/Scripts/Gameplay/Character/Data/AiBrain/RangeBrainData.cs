using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/RangeBrain")]
    public class RangeBrainData : ScriptableObject, IBrainData
    {
        [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField] public float AttackRange { get; private set; } = 10f;

        [field: SerializeField] public float FleeRange { get; private set; } = 3f;

        [field: SerializeField] public float AttackCooldown { get; private set; } = 2f;

        [field: SerializeField] public LayerMask TargetLayer { get; private set; }

        public BTNode BuildTree()
        {
            return new Selector(
                new Sequence(
                    new HasTarget(),
                    new IsInRange(AttackRange),
                    new Flee(FleeRange)
                ),
                new Sequence(
                    new HasTarget(),
                    new IsInRange(AttackRange),
                    new ChaseTarget(AttackRange),
                    new Cooldown(AttackCooldown, new RangedAttack())
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