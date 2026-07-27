using UnityEngine;

namespace _Project.Scripts.Gameplay.Merchant.Data
{
    [CreateAssetMenu(menuName = "Config/Merchant/Recipe Book")]
    public class RecipeBookData : ScriptableObject
    {
        [field: SerializeField]
        public RecipeData[] Recipes { get; private set; }
    }
}
