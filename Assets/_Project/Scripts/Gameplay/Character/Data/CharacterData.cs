using _Project.Scripts.Gameplay.Character.Data.Abilities;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using NaughtyAttributes;
using UnityEngine;


namespace _Project.Scripts.Gameplay.Character.Data
{ //
    [CreateAssetMenu(menuName = "Config/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        //TODO: вытащить из тиров и перенести сюда. В тирах должны остаться только статы.
        //TODO: ни в коем случае не менять название полей иначе вся сериализация слетит.
        //TODO: решить пробелму с мозгами и абилками. Можно добавить абилки но поставить мозг который их не использует и наоборот.
        
        public string Id => name;

        [field: Header("General")]
        [field: SerializeField]
        public string Name { get; private set; } = "Unit";

        [field: SerializeField]
        public float Scale { get; private set; } = 1f;

        [field: SerializeField]
        public RuntimeAnimatorController AnimatorOverride { get; private set; }

        [field: Header("Shop (Gold)")]
        [field: SerializeField]
        public int PriceAsHero { get; private set; } = 100;

        [field: Header("Shop (IAP)")]
        [field: SerializeField]
        public string YandexProductId { get; private set; }

        [field: Header("Tiers")]
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

        [SerializeField]
        public AudioClip[] BaseHitSounds;

        [Button]
        private void SetDefaultHitSounds()
        {
            foreach (var tier in Tiers)
            {
                tier.SetHitSounds(BaseHitSounds);
            }
        }

        [SerializeField]
        public AudioClip[] BaseAttackSounds;

        [Button]
        private void SetDefaultAttackSounds()
        {
            foreach (var tier in Tiers)
            {
                tier.SetAttackSounds(BaseAttackSounds);
            }
        }

#endif
        
    }
}
