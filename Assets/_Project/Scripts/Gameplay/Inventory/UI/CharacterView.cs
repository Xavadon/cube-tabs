using System;
using System.Text;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Inventory.UI
{
    public class CharacterView : UIDocumentView
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

        private IEquipmentService _equipment;
        private ILocalizationService _localization;
        private CharacterEntity _player;

        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipBody;
        private VisualElement _equipmentGrid;
        private VisualElement _backpackGrid;
        private VisualElement _ghost;
        private Label _health;
        private Label _damage;
        private Label _physical;
        private Label _magic;
        private Label _fire;
        private Label _faith;

        private SlotRef _pressed;
        private VisualElement _pressedSlot;
        private VisualElement _hiddenContent;
        private VisualElement _highlighted;
        private Vector2 _pressPosition;
        private int _pointerId;
        private bool _dragging;
        private bool _open;
        private Action _pendingLanding;

        private float _shownHealth = -1f;
        private float _shownMaxHealth = -1f;
        private float _shownDamage = -1f;

        public bool IsOpen => _open;

        public void Bind(IEquipmentService equipment, ILocalizationService localization, CharacterEntity player)
        {
            _equipment = equipment;
            _localization = localization;
            _player = player;

            _equipment.OnChanged += Refresh;

            StartWiring();
        }

        protected override void Wire()
        {
            _equipmentGrid = Root.Q<VisualElement>("equipment");
            _backpackGrid = Root.Q<VisualElement>("backpack");
            _health = Root.Q<Label>("stat-health");
            _damage = Root.Q<Label>("stat-damage");
            _physical = Root.Q<Label>("stat-physical");
            _magic = Root.Q<Label>("stat-magic");
            _fire = Root.Q<Label>("stat-fire");
            _faith = Root.Q<Label>("stat-faith");
            _tooltip = Root.Q<VisualElement>("tooltip");
            _tooltipTitle = Root.Q<Label>("tooltip-title");
            _tooltipBody = Root.Q<Label>("tooltip-body");

            Root.Q<Label>("title").text = _localization.Get(LocalizationKeys.Arpg.CharacterTitle);
            Root.Q<Label>("stats-title").text = _localization.Get(LocalizationKeys.Arpg.StatsSection);
            Root.Q<Label>("equipment-title").text = _localization.Get(LocalizationKeys.Arpg.EquipmentSection);
            Root.Q<Label>("backpack-title").text = _localization.Get(LocalizationKeys.Arpg.BackpackSection);

            Root.Q<Button>("close").clicked += Close;
            Root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            Root.RegisterCallback<PointerUpEvent>(OnPointerUp);

            AbandonDrag();
            ApplyOpenState();
            Refresh();
        }

        private void AbandonDrag()
        {
            _pendingLanding = null;
            _ghost = null;
            _hiddenContent = null;

            ResetDragState();
        }

        private void OnDestroy()
        {
            if (_equipment != null)
                _equipment.OnChanged -= Refresh;
        }

        public void Toggle()
        {
            if (_open)
                Close();
            else
                Open();
        }

        public void Open()
        {
            _open = true;
            ApplyOpenState();
            Refresh();
        }

        public void Close()
        {
            FinishDrag();
            _open = false;
            ApplyOpenState();
        }

        protected override void Update()
        {
            base.Update();

            if (_open)
                RefreshStats();
        }

        private void ApplyOpenState()
        {
            Root.style.display = _open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Refresh()
        {
            if (!_open)
                return;

            InvalidateShownStats();
            RefreshStats();
            RefreshSlots();
        }

        private void InvalidateShownStats()
        {
            _shownHealth = -1f;
            _shownMaxHealth = -1f;
            _shownDamage = -1f;
        }

        private void RefreshStats()
        {
            if (_player == null)
                return;

            CharacterStats stats = _player.Stats;
            if (stats == null)
                return;

            if (Mathf.Approximately(_shownHealth, _player.CurrentHealth)
                && Mathf.Approximately(_shownMaxHealth, _player.MaxHealth)
                && Mathf.Approximately(_shownDamage, stats.Damage))
                return;

            _shownHealth = _player.CurrentHealth;
            _shownMaxHealth = _player.MaxHealth;
            _shownDamage = stats.Damage;

            _health.text = _localization.Get(LocalizationKeys.Arpg.StatHealth,
                _player.CurrentHealth.ToString("F0"), _player.MaxHealth.ToString("F0"));
            _damage.text = _localization.Get(LocalizationKeys.Arpg.StatDamage,
                stats.Damage.ToString("F0"), stats.DamageType);
            _physical.text = _localization.Get(LocalizationKeys.Arpg.StatPhysical, Percent(stats.PhysicalResist));
            _magic.text = _localization.Get(LocalizationKeys.Arpg.StatMagic, Percent(stats.MagicResist));
            _fire.text = _localization.Get(LocalizationKeys.Arpg.StatFire, Percent(stats.FireResist));
            _faith.text = _localization.Get(LocalizationKeys.Arpg.StatFaith, Percent(stats.FaithResist));
        }

        private static string Percent(float value)
        {
            return value.ToString("P0");
        }

        private void RefreshSlots()
        {
            _equipmentGrid.Clear();
            _backpackGrid.Clear();

            for (int i = 0; i < _equipment.SlotCount; i++)
                _equipmentGrid.Add(CreateSlot(_equipment.Slots[i], true, i));

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

            Root.CapturePointer(_pointerId);
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
            HighlightSlotUnder(evt.position);
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

            VisualElement targetSlot = FindSlotElement(Root.panel.Pick(evt.position));
            SlotRef target = targetSlot?.userData as SlotRef;
            bool willApply = CanApply(source, target);
            VisualElement landing = willApply ? targetSlot : _pressedSlot;

            VisualElement ghost = _ghost;
            VisualElement hidden = _hiddenContent;

            _ghost = null;
            _hiddenContent = null;

            ReleaseGesture();

            // предмет остаётся спрятанным в исходном слоте, пока призрак не долетит:
            // иначе он виден и в слоте, и в полёте одновременно
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

        private void HighlightSlotUnder(Vector2 position)
        {
            VisualElement slot = FindSlotElement(Root.panel.Pick(position));
            SetHighlight(CanApply(_pressed, slot?.userData as SlotRef) ? slot : null);
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
            Root.Add(_ghost);
        }

        private void MoveGhost(Vector2 position)
        {
            _ghost.style.left = position.x - _ghost.resolvedStyle.width * 0.5f;
            _ghost.style.top = position.y - _ghost.resolvedStyle.height * 0.5f;
        }

        private void FinishDrag()
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
                Root.ReleasePointer(_pointerId);

            ResetDragState();
        }

        private void FlushPendingLanding()
        {
            _pendingLanding?.Invoke();
        }

        private void ResetDragState()
        {
            _pressed = null;
            _pressedSlot = null;
            _highlighted = null;
            _dragging = false;
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

        private void ShowTooltip(ItemData item, VisualElement slot)
        {
            if (item == null || _dragging)
                return;

            _tooltipTitle.text = ItemName(item);
            _tooltipBody.text = DescribeBonus(item.Bonus);
            _tooltip.style.display = DisplayStyle.Flex;

            Rect bounds = slot.worldBound;
            float left = Mathf.Min(bounds.xMax + TooltipOffset, Root.worldBound.width - _tooltip.resolvedStyle.width);
            _tooltip.style.left = Mathf.Max(0f, left);
            _tooltip.style.top = bounds.yMin;
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

        private void HideTooltip()
        {
            _tooltip.style.display = DisplayStyle.None;
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
