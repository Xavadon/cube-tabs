using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Area")]
    public class AreaAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public float Radius { get; private set; } = 5f;

        [field: SerializeField]
        public float Duration { get; private set; } = 1f;
        
        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 2f, 1f);

        [field: SerializeField]
        public AreaWave Prefab { get; private set; }

        [field: Header("Audio")]
        [field: SerializeField]
        public AudioClip[] Sounds { get; private set; }

        public override void Execute(Blackboard blackboard, float damage, DamageType affectedLayers)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            LayerMask targetLayer = blackboard.Get<LayerMask>(BrainKeys.TargetLayer);

            Vector3 spawnPosition;

            if (caster != null)
            {
                spawnPosition = caster.position + SpawnOffset;
            }
            else if (blackboard.TryGet(BrainKeys.HitPoint, out Vector3 hitPoint))
            {
                spawnPosition = hitPoint + SpawnOffset;
            }
            else
            {
                return;
            }

            // TODO: Заменить Instantiate на пулинг (массовые касты — GC-спайки)
            AreaWave wave = Instantiate(Prefab, spawnPosition, Quaternion.identity);
            wave.Init(Radius, Duration, ApplyMultiplier(damage), affectedLayers, targetLayer);

            if (Sounds is { Length: > 0 })
            {
                Project.Get<ICharacterRegistry>()?.NotifyAbilityUsed(spawnPosition, Sounds);
            }
        }
    }
}
