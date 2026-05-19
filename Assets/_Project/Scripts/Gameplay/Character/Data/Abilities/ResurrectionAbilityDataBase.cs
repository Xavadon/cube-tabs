using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [CreateAssetMenu(menuName = "Config/Abilities/Resurrection")]
    public class ResurrectionAbilityDataBase : AbilityDataBase
    {
        [field: SerializeField]
        public GameObject ResurrectionEffectPrefab { get; private set; }

        [field: SerializeField]
        public Vector3 SpawnOffset { get; private set; } = new(0f, 0f, 1f);

        [field: Header("Audio")]
        [field: SerializeField]
        public AudioClip[] Sounds { get; private set; }

        public override void Execute(Blackboard blackboard, float damage, DamageType damageType)
        {
            Transform caster = blackboard.Get<Transform>(BrainKeys.Transform);
            if (caster == null)
            {
                return;
            }

            if (!caster.TryGetComponent(out Character casterCharacter))
            {
                return;
            }

            IResurrectionService resurrectionService = Project.Get<IResurrectionService>();
            if (resurrectionService == null)
            {
                return;
            }

            Vector3 spawnPosition = caster.position
                                    + caster.forward * SpawnOffset.z
                                    + caster.up * SpawnOffset.y
                                    + caster.right * SpawnOffset.x;

            bool resurrected;
            if (casterCharacter.CharacterType == CharacterType.Ally)
            {
                resurrected = resurrectionService.TryResurrectAllAllies(spawnPosition, caster.rotation);
            }
            else
            {
                resurrected = resurrectionService.TryResurrectAllEnemies(spawnPosition, caster.rotation);
            }

            if (!resurrected)
            {
                return;
            }

            if (ResurrectionEffectPrefab != null)
            {
                Instantiate(ResurrectionEffectPrefab, spawnPosition, Quaternion.identity);
            }

            if (Sounds is { Length: > 0 })
            {
                Project.Get<ICharacterRegistry>()?.NotifyAbilityUsed(spawnPosition, Sounds);
            }
        }
    }
}
