using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Blink")]
    public class BlinkAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public float ArrivalOffset { get; private set; } = 1.5f;

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            var agent = blackboard.Get<NavMeshAgent>(BrainKeys.Agent);
            var caster = blackboard.Get<Transform>(BrainKeys.Transform);
            var target = blackboard.Get<Transform>(BrainKeys.Target);

            if (agent == null || caster == null || target == null)
                return;

            Vector3 direction = (target.position - caster.position).normalized;
            Vector3 blinkPosition = target.position - direction * ArrivalOffset;

            agent.Warp(blinkPosition);
        }
    }
}
