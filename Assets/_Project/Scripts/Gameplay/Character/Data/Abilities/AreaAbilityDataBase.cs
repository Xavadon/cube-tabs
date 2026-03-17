using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
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

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            LayerMask targetLayer = blackboard.Get<LayerMask>(BrainKeys.TargetLayer);

            if (caster == null)
                return;

            // TODO: Заменить Instantiate на пулинг (массовые касты — GC-спайки)
            AreaWave wave = Instantiate(Prefab, caster.position + SpawnOffset, Quaternion.identity);
            wave.Init(Radius, Duration, damage, damageType, targetLayer);
        }
    }
}
