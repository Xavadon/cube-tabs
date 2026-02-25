using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
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
        public LayerMask AffectedLayers { get; private set; }

        [field: SerializeField]
        public AreaWave Prefab { get; private set; }

        public override void Execute(Blackboard blackboard)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);

            if (caster == null)
                return;

            // TODO: Заменить Instantiate на пулинг (массовые касты — GC-спайки)
            AreaWave wave = Instantiate(Prefab, caster.position, Quaternion.identity);
            wave.Init(Radius, Duration, Damage, DamageType, AffectedLayers);
        }
    }
}
