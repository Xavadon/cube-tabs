using System;
using System.Collections.Generic;
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
        private const float GhostSize = 76f;
        private const float GhostIconSize = 56f;

        private enum SlotZone
        {
            Equipment,
            Backpack,
            Craft
        }

        private class SlotRef
        {
            public SlotZone Zone;
            public int Index;
            public ItemData Item;
        }

        private readonly IEquipmentService _equipment;
        private readonly VisualElement _root;
        private readonly VisualElement _equipmentGrid;
        private readonly VisualElement _backpackGrid;
        private readonly VisualElement _craftGrid;
        private readonly ItemTooltip _tooltip;

        private SlotRef _pressed;
        private VisualElement _pressedSlot;
        private VisualElement _hiddenContent;
        private VisualElement _highlighted;
        private VisualElement _ghost;
        private Vector2 _pressPosition;
        private int _pointerId;
        private bool _dragging;
        private Action _pendingLanding;

        // перехватывает короткий клик по предмету; вернул true — быстрое перемещение не выполняется
        public Func<ItemData, bool> ClickInterceptor { get; set; }

        public InventoryPanel(IEquipmentService equipment, ILocalizationService localization, VisualElement root,
            VisualElement equipmentGrid, VisualElement backpackGrid, VisualElement craftGrid = null)
        {
            _equipment = equipment;
            _root = root;
            _equipmentGrid = equipmentGrid;
            _backpackGrid = backpackGrid;
            _craftGrid = craftGrid;
            _tooltip = new ItemTooltip(localization, root);

            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp);

            Refresh();
        }

        public void Refresh()
        {
            Fill(_equipmentGrid, SlotZone.Equipment, _equipment.Slots, _equipment.SlotCount);
            Fill(_backpackGrid, SlotZone.Backpack, _equipment.Backpack, _equipment.BackpackCapacity);
            Fill(_craftGrid, SlotZone.Craft, _equipment.CraftZone, _equipment.CraftCapacity);
        }

        private void Fill(VisualElement grid, SlotZone zone, IReadOnlyList<ItemData> items, int count)
        {
            if (grid == null)
                return;

            grid.Clear();

            for (int i = 0; i < count; i++)
                grid.Add(CreateSlot(items[i], zone, i));
        }

        private VisualElement CreateSlot(ItemData item, SlotZone zone, int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("slot");
            slot.userData = new SlotRef { Zone = zone, Index = index, Item = item };

            if (item == null)
            {
                slot.AddToClassList("slot-empty");
                return slot;
            }

            slot.Add(ItemVisual.CreateContent(item));
            slot.RegisterCallback<PointerDownEvent>(OnPointerDown);
            slot.RegisterCallback<PointerEnterEvent>(_ => ShowTooltip(item, slot));
            slot.RegisterCallback<PointerLeaveEvent>(_ => _tooltip.Hide());

            return slot;
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

            if (source.Zone == target.Zone)
                return source.Index != target.Index;

            // снять в рюкзак можно только в свободную ячейку, остальные переносы — обмен содержимым
            if (source.Zone == SlotZone.Equipment && target.Zone == SlotZone.Backpack)
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
            _tooltip.Hide();

            if (_pressedSlot != null && _pressedSlot.childCount > 0)
            {
                _hiddenContent = _pressedSlot[0];
                _hiddenContent.style.display = DisplayStyle.None;
            }

            _ghost = BuildGhost(_pressed.Item);

            // призрак живёт в корне панели, а не документа: только так он рисуется поверх окна лавки,
            // у которой свой UIDocument с большим sortingOrder. Стили документа туда не достают,
            // поэтому размеры заданы инлайном — иначе иконка разворачивается в натуральный размер спрайта
            VisualElement host = _root.panel != null ? _root.panel.visualTree : _root;
            host.Add(_ghost);
            _ghost.BringToFront();
        }

        private VisualElement BuildGhost(ItemData item)
        {
            var ghost = new VisualElement();
            ghost.AddToClassList("ghost");
            ghost.pickingMode = PickingMode.Ignore;
            ghost.style.position = Position.Absolute;
            ghost.style.width = GhostSize;
            ghost.style.height = GhostSize;
            ghost.style.alignItems = Align.Center;
            ghost.style.justifyContent = Justify.Center;

            VisualElement content = ItemVisual.CreateContent(item);

            if (content is Image)
            {
                content.style.width = GhostIconSize;
                content.style.height = GhostIconSize;
            }
            else
            {
                content.style.width = GhostSize;
                content.style.fontSize = 13;
                content.style.color = Color.white;
                content.style.whiteSpace = WhiteSpace.Normal;
                content.style.unityTextAlign = TextAnchor.MiddleCenter;
            }

            ghost.Add(content);
            return ghost;
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
            if (source.Zone == target.Zone)
            {
                switch (source.Zone)
                {
                    case SlotZone.Equipment:
                        _equipment.SwapSlots(source.Index, target.Index);
                        break;
                    case SlotZone.Backpack:
                        _equipment.MoveInBackpack(source.Index, target.Index);
                        break;
                    case SlotZone.Craft:
                        _equipment.MoveInCraft(source.Index, target.Index);
                        break;
                }

                return;
            }

            if (source.Zone == SlotZone.Backpack && target.Zone == SlotZone.Equipment)
                _equipment.EquipFromBackpack(source.Index, target.Index);
            else if (source.Zone == SlotZone.Equipment && target.Zone == SlotZone.Backpack)
                _equipment.UnequipTo(source.Index, target.Index);
            else if (source.Zone == SlotZone.Backpack && target.Zone == SlotZone.Craft)
                _equipment.BackpackToCraft(source.Index, target.Index);
            else if (source.Zone == SlotZone.Craft && target.Zone == SlotZone.Backpack)
                _equipment.CraftToBackpack(source.Index, target.Index);
            else if (source.Zone == SlotZone.Craft && target.Zone == SlotZone.Equipment)
                _equipment.CraftToEquipment(source.Index, target.Index);
            else if (source.Zone == SlotZone.Equipment && target.Zone == SlotZone.Craft)
                _equipment.EquipmentToCraft(source.Index, target.Index);
        }

        private void QuickMove(SlotRef source)
        {
            if (source.Item != null && ClickInterceptor != null && ClickInterceptor(source.Item))
                return;

            if (source.Zone == SlotZone.Equipment)
            {
                _equipment.Unequip(source.Index);
                return;
            }

            if (source.Zone == SlotZone.Craft)
            {
                int cell = FirstFree(_equipment.Backpack, _equipment.BackpackCapacity);

                if (cell >= 0)
                    _equipment.CraftToBackpack(source.Index, cell);

                return;
            }

            int free = FirstFree(_equipment.Slots, _equipment.SlotCount);

            if (free >= 0)
                _equipment.EquipFromBackpack(source.Index, free);
        }

        private static int FirstFree(IReadOnlyList<ItemData> items, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (items[i] == null)
                    return i;
            }

            return -1;
        }

        private void ShowTooltip(ItemData item, VisualElement slot)
        {
            if (_dragging)
                return;

            _tooltip.Show(item, slot);
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
