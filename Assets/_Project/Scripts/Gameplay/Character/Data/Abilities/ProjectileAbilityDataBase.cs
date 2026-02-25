using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Projectile")]
    public class ProjectileAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public Projectile Prefab { get; private set; }

        [field: SerializeField]
        public float Speed { get; private set; } = 10f;

        public override void Execute(Blackboard blackboard)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            Transform target = blackboard.Get<Transform>(BrainKeys.Target);

            if (caster == null || target == null)
                return;

            Vector3 spawnPosition = caster.position + caster.forward + Vector3.up * 2;
            Projectile projectile = Instantiate(Prefab, spawnPosition, Quaternion.identity);
            projectile.Init(target, Speed, Damage, DamageType);
        }
    }
}
