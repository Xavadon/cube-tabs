using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Bouncing Projectile")]
    public class BouncingProjectileAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public BouncingProjectile Prefab { get; private set; }

        [field: SerializeField]
        public float Speed { get; private set; } = 12f;

        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 2f, 1f);

        [field: SerializeField]
        public int BounceCount { get; private set; } = 3;

        [field: SerializeField]
        public float BounceSearchRadius { get; private set; } = 8f;

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = blackboard.Get<Transform>(BrainKeys.Target);
            LayerMask targetLayer = blackboard.Get<LayerMask>(BrainKeys.TargetLayer);

            if (caster == null || target == null)
                return;

            Vector3 spawnPosition = caster.position
                                    + caster.forward * SpawnOffset.z
                                    + caster.up * SpawnOffset.y
                                    + caster.right * SpawnOffset.x;

            // TODO: Заменить Instantiate на пулинг
            BouncingProjectile projectile = Instantiate(Prefab, spawnPosition, Quaternion.identity);
            projectile.Init(target, Speed, ApplyMultiplier(damage), damageType, BounceCount, BounceSearchRadius, targetLayer);
        }
    }
}
