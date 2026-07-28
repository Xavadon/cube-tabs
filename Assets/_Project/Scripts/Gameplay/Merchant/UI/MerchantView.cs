using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Inventory.UI;
using _Project.Scripts.Gameplay.Merchant.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Merchant.UI
{
    public class MerchantView : UIDocumentView
    {
        private readonly List<MerchantSlot> _stockSlots = new();
        private readonly List<MerchantSlot> _craftableSlots = new();

        private IMerchantService _merchant;
        private IEquipmentService _equipment;
        private IPlayerProgressService _progress;
        private ILocalizationService _localization;

        private ItemTooltip _tooltip;
        private MerchantSlotFactory _slots;
        private RecipeTreeView _tree;
        private InventoryPanel _craftPanel;
        private VisualElement _stock;
        private VisualElement _craftables;
        private Label _gold;
        private bool _open;

        public bool IsOpen => _open;

        public void Bind(IMerchantService merchant, IEquipmentService equipment, IPlayerProgressService progress,
            ILocalizationService localization)
        {
            _merchant = merchant;
            _equipment = equipment;
            _progress = progress;
            _localization = localization;

            _merchant.OnChanged += Refresh;
            _progress.OnGoldChanged += Refresh;
            _equipment.OnChanged += RefreshCraft;

            StartWiring();
        }

        protected override void Wire()
        {
            _stock = Root.Q<VisualElement>("stock");
            _craftables = Root.Q<VisualElement>("craftables");
            _gold = Root.Q<Label>("gold");
            _tooltip = new ItemTooltip(_localization, Root);
            _slots = new MerchantSlotFactory(_merchant, _localization, _tooltip);

            Root.Q<Label>("title").text = _localization.Get(LocalizationKeys.Arpg.MerchantTitle);
            Root.Q<Label>("stock-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantMaterials);
            Root.Q<Label>("craftables-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantCraftables);
            Root.Q<Label>("craft-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantCraft);
            Root.Q<Label>("craft-hint").text = _localization.Get(LocalizationKeys.Arpg.MerchantCraftHint);
            Root.Q<Label>("tree-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantTree);
            Root.Q<Button>("close").clicked += Close;

            _tree = new RecipeTreeView(_merchant, _slots, _localization, Root.Q<VisualElement>("recipe-tree"));

            _craftPanel = new InventoryPanel(_equipment, _localization, Root, null, null, Root.Q<VisualElement>("craft"))
            {
                ClickInterceptor = TryShowRecipe
            };

            BuildShowcases();
            ApplyOpenState();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_merchant != null)
                _merchant.OnChanged -= Refresh;

            if (_progress != null)
                _progress.OnGoldChanged -= Refresh;

            if (_equipment != null)
                _equipment.OnChanged -= RefreshCraft;
        }

        // единая точка входа в дерево: работает только при открытой лавке,
        // поэтому её же вешаем на клики по рюкзаку и слотам HUD
        public bool TryShowRecipe(ItemData item)
        {
            if (!_open || item == null)
                return false;

            _tree.Show(item);
            return true;
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
            _open = false;
            _tooltip.Hide();
            ApplyOpenState();
        }

        private void ApplyOpenState()
        {
            Root.style.display = _open ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Refresh()
        {
            if (!_open)
                return;

            _gold.text = _localization.Get(LocalizationKeys.Arpg.MerchantGold, _progress.Gold);

            _slots.RefreshAffordable(_stockSlots);
            _slots.RefreshAffordable(_craftableSlots);
            _tree.RefreshAffordable();
        }

        private void RefreshCraft()
        {
            _craftPanel?.Refresh();
        }

        // витрины неизменны, пересобирать их на каждую покупку незачем:
        // ячейка живёт от Wire до Wire, меняется только подсветка цены
        private void BuildShowcases()
        {
            var materials = new List<ItemData>();

            foreach (MerchantEntry entry in _merchant.Stock)
            {
                if (entry?.Item != null)
                    materials.Add(entry.Item);
            }

            Fill(_stock, _stockSlots, materials);
            Fill(_craftables, _craftableSlots, _merchant.Craftables);
        }

        private void Fill(VisualElement grid, List<MerchantSlot> slots, IReadOnlyList<ItemData> items)
        {
            grid.Clear();
            slots.Clear();

            foreach (ItemData item in items)
            {
                if (item == null)
                    continue;

                MerchantSlot slot = _slots.Create(item, selected => TryShowRecipe(selected));

                slots.Add(slot);
                grid.Add(slot.Element);
            }
        }
    }
}
