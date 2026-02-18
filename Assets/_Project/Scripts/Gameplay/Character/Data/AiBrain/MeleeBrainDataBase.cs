using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.BehaviorTree.Composites;
using _Project.Scripts.Architecture.BehaviorTree.Decorators;
using _Project.Scripts.Architecture.BehaviorTree.Leaves;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/MeleeBrain")]
    public class MeleeBrainDataBase : BrainDataBase
    {
        [field: SerializeField] 
        public float DetectionRadius { get; private set; } = 10f;

        [field: SerializeField] 
        public float WindUpDuration { get; private set; } = 0.3f;
        
        [field: SerializeField] 
        public float AttackRange { get; private set; } = 2f;

        [field: SerializeField] 
        public float AttackCooldown { get; private set; } = 1.5f;

        [field: SerializeField] 
        public LayerMask TargetLayer { get; private set; }

        public override BTNode BuildTree()
        {
            return new Selector(
                new Sequence(
                    new HasTarget(),
                    new IsInRange(AttackRange),
                    new Cooldown(AttackCooldown, new MeleeAttack(WindUpDuration,AttackRange))
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