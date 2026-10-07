using _Project.Scripts.Architecture.Services;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Kit
{
    public static class PriceText
    {
        public static void Apply(Label label, VisualElement yanIcon, string price)
        {
            bool isYan = price != null && price.Contains(PriceTags.Yan);

            if (isYan)
            {
                label.text = price.Replace(PriceTags.Yan, string.Empty).Trim();
            }
            else
            {
                label.text = price;
            }

            yanIcon.SetDisplayed(isYan);
        }
    }
}
