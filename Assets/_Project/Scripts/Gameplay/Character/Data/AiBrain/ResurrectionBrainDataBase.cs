using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/ResurrectionBrain")]
    public class ResurrectionBrainDataBase : BrainDataBase
    {
        [field: SerializeField]
        public float DetectionRadius { get; private set; } = 15f;

        [field: SerializeField]
        public float ResurrectionCooldown { get; private set; } = 30f;

        [field: SerializeField]
        public float WindUpDuration { get; private set; } = 0.5f;

        [field: SerializeField]
        public float AttackDuration { get; private set; } = 2f;

        [field: SerializeField]
        public float FollowDistance { get; private set; } = 5f;

        [field: SerializeField]
        public LayerMask AllyLayer { get; private set; }

        public override BTNode BuildTree(LayerMask targetLayer, TierData tier)
        {
            return new Selector
            (
                new Cooldown(ResurrectionCooldown,
                    new Sequence
                    (
                        new HasDeadAlly(),
                        new AbilityAttack(tier.Ability, WindUpDuration, AttackDuration, 0f)
                    )
                ),
                new Parallel
                (
                    new FindClosestAlly(DetectionRadius, AllyLayer),
                    new ChaseTarget(FollowDistance)
                ),
                new Idle()
            );
        }
    }
}
