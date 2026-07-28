using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Inventory.UI
{
    public static class ItemVisual
    {
        public static VisualElement CreateContent(ItemData item)
        {
            if (item.Icon != null)
            {
                var icon = new Image { sprite = item.Icon };
                icon.AddToClassList("slot-icon");
                icon.pickingMode = PickingMode.Ignore;
                return icon;
            }

            var label = new Label(item.DisplayName);
            label.AddToClassList("slot-label");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }
    }
}
