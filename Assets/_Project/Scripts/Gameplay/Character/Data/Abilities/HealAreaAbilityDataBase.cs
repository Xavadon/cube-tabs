using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Heal Area")]
    public class HealAreaAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public float Radius { get; private set; } = 5f;

        [field: SerializeField]
        public float Duration { get; private set; } = 1f;

        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 0.1f, 0f);

        [field: SerializeField]
        public HealWave Prefab { get; private set; }

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);

            if (caster == null)
                return;

            // Союзники находятся на том же слое, что и кастер
            LayerMask allyLayer = 1 << caster.gameObject.layer;

            // TODO: Заменить Instantiate на пулинг (массовые касты — GC-спайки)
            HealWave wave = Instantiate(Prefab, caster.position + SpawnOffset, Quaternion.identity);
            wave.Init(Radius, Duration, damage, allyLayer);
        }
    }
}
