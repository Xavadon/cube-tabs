using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Kit
{
    public static class VisualElementExtensions
    {
        public static void SetDisplayed(this VisualElement element, bool displayed)
        {
            if (displayed)
            {
                element.style.display = DisplayStyle.Flex;
            }
            else
            {
                element.style.display = DisplayStyle.None;
            }
        }

        public static void SetVisible(this VisualElement element, bool visible)
        {
            if (visible)
            {
                element.style.visibility = Visibility.Visible;
            }
            else
            {
                element.style.visibility = Visibility.Hidden;
            }
        }

        public static void SetImage(this VisualElement element, RenderTexture texture)
        {
            if (texture == null)
            {
                element.style.backgroundImage = StyleKeyword.None;
                return;
            }

            element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(texture));
        }

        public static void SetImage(this VisualElement element, Sprite sprite)
        {
            if (sprite == null)
            {
                element.style.backgroundImage = StyleKeyword.None;
                return;
            }

            element.style.backgroundImage = new StyleBackground(sprite);
        }

        public static T AddChild<T>(this VisualElement parent, T child, string className) where T : VisualElement
        {
            child.AddToClassList(className);
            parent.Add(child);
            return child;
        }
    }
}
