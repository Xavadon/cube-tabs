using _Project.Scripts.Gameplay.Inventory;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Merchant.Data
{
    [CreateAssetMenu(menuName = "Config/Merchant/Recipe")]
    public class RecipeData : ScriptableObject
    {
        [field: SerializeField]
        public ItemData Result { get; private set; }

        [field: SerializeField]
        public ItemData[] Ingredients { get; private set; }
    }
}
