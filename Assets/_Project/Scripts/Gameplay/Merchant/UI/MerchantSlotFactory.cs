using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Inventory.UI;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Merchant.UI
{
    public class MerchantSlot
    {
        public ItemData Item;
        public VisualElement Element;
    }

    public class MerchantSlotFactory
    {
        private const string CellClass = "shop-cell";
        private const string AffordableClass = "shop-cell--affordable";
        private const string SelectedClass = "shop-cell--selected";
        private const string PriceColor = "#8A6612";

        private readonly IMerchantService _merchant;
        private readonly ILocalizationService _localization;
        private readonly ItemTooltip _tooltip;

        public MerchantSlotFactory(IMerchantService merchant, ILocalizationService localization, ItemTooltip tooltip)
        {
            _merchant = merchant;
            _localization = localization;
            _tooltip = tooltip;
        }

        public MerchantSlot Create(ItemData item, Action<ItemData> onSelect, bool selected = false)
        {
            var cell = new VisualElement();
            cell.AddToClassList(CellClass);

            if (selected)
                cell.AddToClassList(SelectedClass);

            var slot = new VisualElement();
            slot.AddToClassList("slot");
            slot.pickingMode = PickingMode.Ignore;
            slot.Add(ItemVisual.CreateContent(item));
            cell.Add(slot);

            cell.RegisterCallback<PointerDownEvent>(evt => HandlePointerDown(evt, item, onSelect));
            cell.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(item, cell));
            cell.RegisterCallback<PointerLeaveEvent>(_ => _tooltip.Hide());

            return new MerchantSlot { Item = item, Element = cell };
        }

        public void RefreshAffordable(IReadOnlyList<MerchantSlot> slots)
        {
            foreach (MerchantSlot slot in slots)
            {
                if (_merchant.CanBuy(slot.Item))
                    slot.Element.AddToClassList(AffordableClass);
                else
                    slot.Element.RemoveFromClassList(AffordableClass);
            }
        }

        private void HandlePointerDown(PointerDownEvent evt, ItemData item, Action<ItemData> onSelect)
        {
            if (evt.button == (int)MouseButton.RightMouse)
            {
                _merchant.Buy(item);
                evt.StopPropagation();
                return;
            }

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            onSelect?.Invoke(item);
            evt.StopPropagation();
        }

        private void ShowTooltip(ItemData item, VisualElement anchor)
        {
            int price = _merchant.PriceOf(item);

            string footer = price > 0
                ? $"<b><color={PriceColor}>{_localization.Get(LocalizationKeys.Arpg.MerchantPrice, price)}</color></b>"
                : null;

            _tooltip.Show(item, anchor, footer);
        }
    }
}
