using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Projectile")]
    public class ProjectileAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public Projectile Prefab { get; private set; }

        [field: SerializeField]
        public bool DealDirectDamage { get; private set; } = true;
        
        [field: SerializeField]
        public float Speed { get; private set; } = 10f;

        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 2f, 1f);

        [field: SerializeField]
        public AbilityDataBase OnHitAbility { get; private set; }

        public override void Execute(Blackboard blackboard, float damage, DamageType affectedLayers)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = blackboard.Get<Transform>(BrainKeys.Target);

            if (caster == null || target == null)
                return;

            Projectile projectile = SpawnProjectile(caster);

            float projectileDamage = DealDirectDamage ? damage : 0f;

            projectile.Init(target, Speed, projectileDamage, affectedLayers, onHit: OnHitAbility == null ? null : _ =>
            {
                Transform originalTransform = blackboard.Get<Transform>(BrainKeys.Transform);
                blackboard.Set(BrainKeys.Transform, target);
                OnHitAbility.Execute(blackboard, damage, affectedLayers);
                blackboard.Set(BrainKeys.Transform, originalTransform);
            });
        }

        // TODO: Заменить Instantiate на пулинг (орды лучников — десятки проджектайлов одновременно, GC-спайки)
        protected Projectile SpawnProjectile(Transform caster)
        {
            Vector3 spawnPosition = caster.position
                                    + caster.forward * SpawnOffset.z
                                    + caster.up * SpawnOffset.y
                                    + caster.right * SpawnOffset.x;

            return Instantiate(Prefab, spawnPosition, Quaternion.identity);
        }
    }
}
