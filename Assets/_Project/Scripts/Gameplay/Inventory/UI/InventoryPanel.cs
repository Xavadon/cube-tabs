using System;
using System.Text;
using _Project.Scripts.Architecture.Services.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace _Project.Scripts.Gameplay.Inventory.UI
{
    public class InventoryPanel
    {
        private const float DragThreshold = 6f;
        private const int DropAnimationMs = 120;
        private const float TooltipOffset = 12f;

        private class SlotRef
        {
            public bool IsEquipment;
            public int Index;
            public ItemData Item;
        }

        private readonly IEquipmentService _equipment;
        private readonly ILocalizationService _localization;
        private readonly VisualElement _root;
        private readonly VisualElement _equipmentGrid;
        private readonly VisualElement _backpackGrid;
        private readonly VisualElement _tooltip;
        private readonly Label _tooltipTitle;
        private readonly Label _tooltipBody;

        private SlotRef _pressed;
        private VisualElement _pressedSlot;
        private VisualElement _hiddenContent;
        private VisualElement _highlighted;
        private VisualElement _ghost;
        private Vector2 _pressPosition;
        private int _pointerId;
        private bool _dragging;
        private Action _pendingLanding;

        public InventoryPanel(IEquipmentService equipment, ILocalizationService localization, VisualElement root,
            VisualElement equipmentGrid, VisualElement backpackGrid)
        {
            _equipment = equipment;
            _localization = localization;
            _root = root;
            _equipmentGrid = equipmentGrid;
            _backpackGrid = backpackGrid;

            _tooltip = root.Q<VisualElement>("tooltip");
            _tooltipTitle = root.Q<Label>("tooltip-title");
            _tooltipBody = root.Q<Label>("tooltip-body");

            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);

            Refresh();
        }

        public void Refresh()
        {
            if (_equipmentGrid != null)
            {
                _equipmentGrid.Clear();

                for (int i = 0; i < _equipment.SlotCount; i++)
                    _equipmentGrid.Add(CreateSlot(_equipment.Slots[i], true, i));
            }

            if (_backpackGrid == null)
                return;

            _backpackGrid.Clear();

            for (int i = 0; i < _equipment.BackpackCapacity; i++)
                _backpackGrid.Add(CreateSlot(_equipment.Backpack[i], false, i));
        }

        private VisualElement CreateSlot(ItemData item, bool isEquipment, int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("slot");
            slot.userData = new SlotRef { IsEquipment = isEquipment, Index = index, Item = item };

            if (item == null)
            {
                slot.AddToClassList("slot-empty");
                return slot;
            }

            slot.Add(BuildItemContent(item));
            slot.RegisterCallback<PointerDownEvent>(OnPointerDown);
            slot.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(item, slot));
            slot.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());

            return slot;
        }

        private VisualElement BuildItemContent(ItemData item)
        {
            if (item.Icon != null)
            {
                var icon = new Image { sprite = item.Icon };
                icon.AddToClassList("slot-icon");
                icon.pickingMode = PickingMode.Ignore;
                return icon;
            }

            var label = new Label(ItemName(item));
            label.AddToClassList("slot-label");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private static string ItemName(ItemData item)
        {
            return string.IsNullOrEmpty(item.Name) ? item.name : item.Name;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement element || element.userData is not SlotRef slot)
                return;

            FlushPendingLanding();

            _pressed = slot;
            _pressedSlot = element;
            _pressPosition = evt.position;
            _pointerId = evt.pointerId;
            _dragging = false;

            _root.CapturePointer(_pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_pressed == null)
                return;

            if (!_dragging && Vector2.Distance(evt.position, _pressPosition) >= DragThreshold)
                BeginDrag();

            if (!_dragging)
                return;

            MoveGhost(evt.position);
            SetHighlight(FindApplicableSlot(evt.position));
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_pressed == null)
                return;

            SlotRef source = _pressed;

            if (!_dragging)
            {
                FinishDrag();
                QuickMove(source);
                return;
            }

            VisualElement targetSlot = FindSlotElement(_root.panel.Pick(evt.position));
            SlotRef target = targetSlot?.userData as SlotRef;
            bool willApply = CanApply(source, target);
            VisualElement landing = willApply ? targetSlot : _pressedSlot;

            VisualElement ghost = _ghost;
            VisualElement hidden = _hiddenContent;

            _ghost = null;
            _hiddenContent = null;

            ReleaseGesture();

            // предмет прячется в исходном слоте, пока призрак летит: иначе он виден дважды
            _pendingLanding = () =>
            {
                _pendingLanding = null;

                ghost?.RemoveFromHierarchy();

                if (willApply)
                    ApplyDrop(source, target);
                else if (hidden != null)
                    hidden.style.display = DisplayStyle.Flex;
            };

            FlyGhost(ghost, landing);
        }

        private void FlyGhost(VisualElement ghost, VisualElement landing)
        {
            if (ghost == null || landing == null)
            {
                _pendingLanding?.Invoke();
                return;
            }

            Vector2 from = new(ghost.resolvedStyle.left, ghost.resolvedStyle.top);
            Rect bounds = landing.worldBound;
            Vector2 to = new(
                bounds.center.x - ghost.resolvedStyle.width * 0.5f,
                bounds.center.y - ghost.resolvedStyle.height * 0.5f);

            ghost.experimental.animation
                .Start(0f, 1f, DropAnimationMs, (element, t) =>
                {
                    element.style.left = Mathf.Lerp(from.x, to.x, t);
                    element.style.top = Mathf.Lerp(from.y, to.y, t);
                })
                .Ease(Easing.OutQuad)
                .OnCompleted(() => _pendingLanding?.Invoke());
        }

        private VisualElement FindApplicableSlot(Vector2 position)
        {
            VisualElement slot = FindSlotElement(_root.panel.Pick(position));
            return CanApply(_pressed, slot?.userData as SlotRef) ? slot : null;
        }

        private bool CanApply(SlotRef source, SlotRef target)
        {
            if (source == null || target == null || source == target)
                return false;

            if (source.IsEquipment == target.IsEquipment)
                return source.Index != target.Index;

            if (source.IsEquipment)
                return target.Item == null;

            return true;
        }

        private void SetHighlight(VisualElement slot)
        {
            if (_highlighted == slot)
                return;

            _highlighted?.RemoveFromClassList("slot-hover");
            _highlighted = slot;
            _highlighted?.AddToClassList("slot-hover");
        }

        private void BeginDrag()
        {
            _dragging = true;
            HideTooltip();

            if (_pressedSlot != null && _pressedSlot.childCount > 0)
            {
                _hiddenContent = _pressedSlot[0];
                _hiddenContent.style.display = DisplayStyle.None;
            }

            _ghost = new VisualElement();
            _ghost.AddToClassList("ghost");
            _ghost.pickingMode = PickingMode.Ignore;
            _ghost.Add(BuildItemContent(_pressed.Item));
            _root.Add(_ghost);
        }

        private void MoveGhost(Vector2 position)
        {
            _ghost.style.left = position.x - _ghost.resolvedStyle.width * 0.5f;
            _ghost.style.top = position.y - _ghost.resolvedStyle.height * 0.5f;
        }

        public void FinishDrag()
        {
            FlushPendingLanding();

            if (_ghost != null)
            {
                _ghost.RemoveFromHierarchy();
                _ghost = null;
            }

            if (_hiddenContent != null)
            {
                _hiddenContent.style.display = DisplayStyle.Flex;
                _hiddenContent = null;
            }

            ReleaseGesture();
        }

        private void ReleaseGesture()
        {
            SetHighlight(null);

            if (_pressed != null)
                _root.ReleasePointer(_pointerId);

            _pressed = null;
            _pressedSlot = null;
            _dragging = false;
        }

        private void FlushPendingLanding()
        {
            _pendingLanding?.Invoke();
        }

        private void ApplyDrop(SlotRef source, SlotRef target)
        {
            if (source.IsEquipment && target.IsEquipment)
                _equipment.SwapSlots(source.Index, target.Index);
            else if (target.IsEquipment)
                _equipment.EquipFromBackpack(source.Index, target.Index);
            else if (source.IsEquipment)
                _equipment.UnequipTo(source.Index, target.Index);
            else
                _equipment.MoveInBackpack(source.Index, target.Index);
        }

        private void QuickMove(SlotRef source)
        {
            if (source.IsEquipment)
            {
                _equipment.Unequip(source.Index);
                return;
            }

            int free = FirstFreeSlot();

            if (free >= 0)
                _equipment.EquipFromBackpack(source.Index, free);
        }

        private int FirstFreeSlot()
        {
            for (int i = 0; i < _equipment.SlotCount; i++)
            {
                if (_equipment.Slots[i] == null)
                    return i;
            }

            return -1;
        }

        private void ShowTooltip(ItemData item, VisualElement slot)
        {
            if (item == null || _dragging || _tooltip == null)
                return;

            _tooltipTitle.text = ItemName(item);
            _tooltipBody.text = DescribeBonus(item.Bonus);
            _tooltip.style.display = DisplayStyle.Flex;

            Rect bounds = slot.worldBound;
            float left = Mathf.Min(bounds.xMax + TooltipOffset, _root.worldBound.width - _tooltip.resolvedStyle.width);
            _tooltip.style.left = Mathf.Max(0f, left);
            _tooltip.style.top = Mathf.Max(0f, bounds.yMin - _tooltip.resolvedStyle.height - TooltipOffset);
        }

        private void HideTooltip()
        {
            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;
        }

        private string DescribeBonus(StatBonus bonus)
        {
            if (bonus == null)
                return _localization.Get(LocalizationKeys.Arpg.BonusNone);

            var builder = new StringBuilder();

            AppendBonus(builder, LocalizationKeys.Arpg.BonusDamage, bonus.Damage, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusHealth, bonus.Health, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusPhysical, bonus.PhysicalResist, true);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusMagic, bonus.MagicResist, true);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusFire, bonus.FireResist, true);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusFaith, bonus.FaithResist, true);

            return builder.Length > 0 ? builder.ToString() : _localization.Get(LocalizationKeys.Arpg.BonusNone);
        }

        private void AppendBonus(StringBuilder builder, string key, float value, bool percent)
        {
            if (Mathf.Approximately(value, 0f))
                return;

            if (builder.Length > 0)
                builder.Append('\n');

            string amount = percent
                ? (value > 0 ? "+" : "") + (value * 100f).ToString("0.#") + "%"
                : (value > 0 ? "+" : "") + value.ToString("0.#");

            builder.Append(_localization.Get(key, amount));
        }

        private VisualElement FindSlotElement(VisualElement element)
        {
            while (element != null)
            {
                if (element.userData is SlotRef)
                    return element;

                element = element.parent;
            }

            return null;
        }
    }
}
