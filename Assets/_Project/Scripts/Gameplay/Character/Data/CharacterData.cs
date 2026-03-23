using _Project.Scripts.Gameplay.Character.Data.Abilities;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using NaughtyAttributes;
using UnityEngine;


namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        public string Id => name;

        [field: SerializeField]
        public string Name { get; private set; } = "Unit";

        [field: SerializeField]
        public int PriceAsHero { get; private set; } = 100;

        [field: SerializeField]
        public TierData[] Tiers { get; private set; }

        public int MaxTier => Tiers is { Length: > 0 } ? Tiers.Length - 1 : 0;

        public TierData GetTier(int tierIndex)
        {
            if (Tiers == null || Tiers.Length == 0)
            {
                Debug.LogError($"[CharacterData] '{Name}' (Id={Id}) has no Tiers configured!");
                return null;
            }

            return Tiers[Mathf.Clamp(tierIndex, 0, Tiers.Length - 1)];
        }
        
#if UNITY_EDITOR

        [Header("Editor Defaults")]
        [SerializeField] 
        public Material BaseMaterial;

        [Button]
        private void SetDefaultMaterial()
        {
            foreach (var tier in Tiers)
            {
                tier.SetMaterial(BaseMaterial);
            }
        }

        [SerializeField]
        public WeaponData BaseWeapon;

        [Button]
        private void SetDefaultWeapon()
        {
            foreach (var tier in Tiers)
            {
                tier.SetWeapon(BaseWeapon);
            }
        }

        [SerializeField]
        public AbilityDataBase BaseAbility;

        [Button]
        private void SetDefaultAbility()
        {
            foreach (var tier in Tiers)
            {
                tier.SetAbility(BaseAbility);
            }
        }

        [SerializeField] 
        public BrainDataBase BaseBrain;

        [Button]
        private void SetDefaultBrain()
        {
            foreach (var tier in Tiers)
            {
                tier.SetBrain(BaseBrain);
            }
        }

#endif
        
    }
}
