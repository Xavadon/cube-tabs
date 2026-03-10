using NaughtyAttributes;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [field: SerializeField]
        public int Id { get; private set; }

        [field: SerializeField]
        public string Name { get; private set; } = "Unit";

        [field: SerializeField]
        public int Price { get; private set; } = 100;

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
        
        [Header("Editor")] [SerializeField]
        public Material BaseMaterial;

        [Button]
        private void SetDefaultMaterial()
        {
            foreach (var tier in Tiers)
            {
                tier.SetMaterial(BaseMaterial);
            }
        }
        
#endif
        
    }
}
