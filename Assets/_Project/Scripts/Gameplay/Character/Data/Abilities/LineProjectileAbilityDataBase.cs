using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Line Projectile (Zoltraak)")]
    public class LineProjectileAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public LineProjectile Prefab { get; private set; }

        [field: SerializeField]
        public float Speed { get; private set; } = 20f;

        [field: SerializeField]
        public float MaxDistance { get; private set; } = 30f;

        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 1.5f, 1f);

        [field: Header("Audio")]
        [field: SerializeField]
        public AudioClip[] Sounds { get; private set; }

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = blackboard.Get<Transform>(BrainKeys.Target);
            LayerMask targetLayer = blackboard.Get<LayerMask>(BrainKeys.TargetLayer);

            if (caster == null || target == null)
            {
                return;
            }

            Vector3 spawnPosition = caster.position
                                    + caster.forward * SpawnOffset.z
                                    + caster.up * SpawnOffset.y
                                    + caster.right * SpawnOffset.x;

            Vector3 direction = target.position - spawnPosition;
            direction.y = 0f;
            direction.Normalize();

            // TODO: Pooling
            LineProjectile projectile = Instantiate(Prefab, spawnPosition, Quaternion.identity);
            projectile.Init(direction, Speed, MaxDistance, ApplyMultiplier(damage), damageType, targetLayer);

            if (Sounds is { Length: > 0 })
            {
                Project.Get<ICharacterRegistry>()?.NotifyAbilityUsed(spawnPosition, Sounds);
            }
        }
    }
}
