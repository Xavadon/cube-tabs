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
        public CharacterStatsData Stats { get; private set; }

        [field: SerializeField]
        public CharacterResistancesData Resistances { get; private set; }

        [field: SerializeField]
        public WeaponData[] WeaponData { get; private set; }

        [field: SerializeField]
        public BrainDataBase BrainData { get; private set; }

        [field: SerializeField]
        public AbilityDataBase Ability { get; private set; }

        [field: SerializeField]
        public AbilityAnimationType AnimationType { get; private set; }

        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;

        [field: SerializeField]
        public int KillReward { get; private set; } = 10;

        [field: SerializeField]
        public int EvolutionCost { get; private set; } = 100;

        [field: SerializeField]
        public Material SkinMaterial { get; private set; }

        [field: SerializeField]
        public Material ArmorMaterial { get; private set; }

        //TODO: изучить подходы работы с датой
        
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

        public void SetAbilityAnimation(AbilityAnimationType animationType)
        {
            AnimationType = animationType;
        }
        
        public void SetWeapon(WeaponData weaponData)
        {
            WeaponData = new[] { weaponData };
        }
        
#endif
    }
}
