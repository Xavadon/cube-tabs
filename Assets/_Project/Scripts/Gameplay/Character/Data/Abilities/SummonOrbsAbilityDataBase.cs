using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/SummonOrbs")]
    public class SummonOrbsAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public ShootingOrb OrbPrefab { get; private set; }

        [field: SerializeField]
        public int OrbCount { get; private set; } = 3;

        [field: SerializeField]
        public float OrbLifetime { get; private set; } = 5f;

        [field: SerializeField]
        public float SpawnRadius { get; private set; } = 2f;

        [field: SerializeField]
        public float SpawnHeight { get; private set; } = 1.5f;

        [field: SerializeField]
        public ProjectileAbilityDataBase ProjectileAbility { get; private set; }

        [field: SerializeField]
        public float DetectionRadius { get; private set; } = 10f;

        [field: SerializeField]
        public float AttackCooldown { get; private set; } = 1f;

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            LayerMask targetLayer = blackboard.Get<LayerMask>(BrainKeys.TargetLayer);

            if (caster == null)
                return;

            float scaledDamage = ApplyMultiplier(damage);
            float angleStep = 360f / OrbCount;

            for (int i = 0; i < OrbCount; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;
                Vector3 spawnPos = caster.position + offset + Vector3.up * SpawnHeight;

                // TODO: Заменить Instantiate на пулинг
                ShootingOrb orb = Instantiate(OrbPrefab, spawnPos, Quaternion.identity);
                orb.Init(
                    ProjectileAbility,
                    DetectionRadius,
                    AttackCooldown,
                    scaledDamage,
                    damageType,
                    targetLayer,
                    OrbLifetime
                );
            }
        }
    }
}
