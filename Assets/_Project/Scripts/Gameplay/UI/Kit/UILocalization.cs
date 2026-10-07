using _Project.Scripts.Architecture.Services.Localization;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Kit
{
    public static class UILocalization
    {
        public const string LocalizedClass = "loc";

        public static void Apply(VisualElement root, ILocalizationService localization)
        {
            foreach (var element in root.Query<TextElement>(className: LocalizedClass).Build())
            {
                var key = element.userData as string;

                if (key == null)
                {
                    key = element.text;
                    element.userData = key;
                }

                element.text = localization.Get(key);
            }
        }
    }
}
