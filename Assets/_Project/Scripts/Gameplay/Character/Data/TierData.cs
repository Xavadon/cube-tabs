using System;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [Serializable]
    public class TierData
    {
        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;

        [field: SerializeField]
        public CharacterStatsData Stats { get; private set; }

        [field: SerializeField]
        public WeaponData[] WeaponData { get; private set; }

        [field: SerializeField]
        public BrainDataBase BrainData { get; private set; }

        [field: SerializeField]
        public AbilityDataBase Ability { get; private set; }

        [field: SerializeField]
        public AbilityDataBase DeathAbility { get; private set; }

        [field: SerializeField]
        public int KillReward { get; private set; } = 10;

        [field: SerializeField]
        public int ExpReward { get; private set; } = 5;

        [field: SerializeField]
        public int EvolutionCost { get; private set; } = 100;

        [field: SerializeField]
        public Material SkinMaterial { get; private set; }

        [field: SerializeField]
        public Material ArmorMaterial { get; private set; }

        [field: Header("Audio")]
        [field: SerializeField]
        public AudioClip[] HitSounds { get; private set; }

        [field: SerializeField]
        public AudioClip[] AttackSounds { get; private set; }
        
#if UNITY_EDITOR

        public void SetMaterial(Material material)
        {
            SkinMaterial = material;
            ArmorMaterial = material;
        }

        public void SetBrain(BrainDataBase brain)
        {
            BrainData = brain;
        }
        
        public void SetAbility(AbilityDataBase ability)
        {
            Ability = ability;
        }

        public void SetWeapon(WeaponData weaponData)
        {
            WeaponData = new[] { weaponData };
        }

        public void SetHitSounds(AudioClip[] hitSounds)
        {
            HitSounds = hitSounds;
        }

        public void SetAttackSounds(AudioClip[] attackSounds)
        {
            AttackSounds = attackSounds;
        }

#endif
    }
}
