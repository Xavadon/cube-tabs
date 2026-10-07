using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Summon")]
    public class SummonAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public CharacterData SummonData { get; private set; }

        [field: SerializeField]
        public int TierIndex { get; private set; }

        [field: SerializeField]
        public int Count { get; private set; } = 2;

        [field: SerializeField]
        public float SpawnRadius { get; private set; } = 3f;

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            if (caster == null)
                return;

            var casterCharacter = caster.GetComponent<Character>();
            if (casterCharacter == null)
                return;

            var factory = Project.Get<ICharacterFactory>();
            var registry = Project.Get<ICharacterRegistry>();

            for (int i = 0; i < Count; i++)
            {
                Character minion = factory.Create(casterCharacter.CharacterType, SummonData, TierIndex);
                minion.SetRegistry(registry);
                registry.Register(minion);

                Vector2 offset = Random.insideUnitCircle * SpawnRadius;
                Vector3 spawnPos = caster.position + new Vector3(offset.x, 0f, offset.y);
                minion.transform.SetPositionAndRotation(spawnPos, caster.rotation);
            }
        }
    }
}
