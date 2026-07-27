using System;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Inventory.UI;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine;
using UnityEngine.UIElements;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Dev
{
    public class HudView : UIDocumentView
    {
        private IPlayerProgressService _progress;
        private IEquipmentService _equipment;
        private ILocalizationService _localization;
        private IUnitPreviewService _preview;
        private CharacterEntity _player;
        private InventoryPanel _inventory;

        private Label _goldValue;
        private Label _expValue;
        private VisualElement _hpFill;
        private Label _hpText;
        private Label _damage;
        private Label _physical;
        private Label _magic;
        private Label _fire;
        private VisualElement _backpackPanel;
        private Button _backpackToggle;

        private bool _backpackOpen;
        private float _shownHealth = -1f;
        private float _shownMaxHealth = -1f;
        private float _shownDamage = -1f;

        public event Action OnMerchantClicked;

        public void Bind(IPlayerProgressService progress, IEquipmentService equipment,
            ILocalizationService localization, IUnitPreviewService preview, CharacterEntity player)
        {
            _progress = progress;
            _equipment = equipment;
            _localization = localization;
            _preview = preview;
            _player = player;

            _progress.OnGoldChanged += RefreshGold;
            _progress.OnExpChanged += RefreshExp;
            _equipment.OnChanged += RefreshInventory;

            StartWiring();
        }

        protected override void Wire()
        {
            _goldValue = Root.Q<Label>("gold-value");
            _expValue = Root.Q<Label>("exp-value");
            _hpFill = Root.Q<VisualElement>("hp-fill");
            _hpText = Root.Q<Label>("hp-text");
            _damage = Root.Q<Label>("stat-damage");
            _physical = Root.Q<Label>("stat-physical");
            _magic = Root.Q<Label>("stat-magic");
            _fire = Root.Q<Label>("stat-fire");
            _backpackPanel = Root.Q<VisualElement>("backpack-panel");
            _backpackToggle = Root.Q<Button>("backpack-toggle");

            Root.Q<Label>("backpack-title").text = _localization.Get(LocalizationKeys.Arpg.BackpackSection);

            var merchantButton = Root.Q<Button>("shop-button");
            merchantButton.text = _localization.Get(LocalizationKeys.Arpg.HudMerchant);
            merchantButton.clicked += () => OnMerchantClicked?.Invoke();

            _backpackToggle.clicked += ToggleBackpack;

            _inventory = new InventoryPanel(_equipment, _localization, Root,
                Root.Q<VisualElement>("equipment"), Root.Q<VisualElement>("backpack"));

            ApplyBackpackState();
            ApplyPortrait();
            InvalidateShownStats();
            RefreshGold();
            RefreshExp();
            RefreshBackpackCount();
        }

        private void ApplyPortrait()
        {
            var portrait = Root.Q<VisualElement>("portrait");

            if (portrait == null || _preview == null || _player == null || _player.CharacterDataRef == null)
                return;

            RenderTexture texture = _preview.GetPortrait(_player.CharacterDataRef, _player.TierIndex);

            if (texture != null)
                portrait.style.backgroundImage = Background.FromRenderTexture(texture);
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
                _progress.OnExpChanged -= RefreshExp;
            }

            if (_equipment != null)
                _equipment.OnChanged -= RefreshInventory;
        }

        protected override void Update()
        {
            base.Update();

            RefreshStats();
        }

        private void ToggleBackpack()
        {
            _backpackOpen = !_backpackOpen;
            ApplyBackpackState();
        }

        private void ApplyBackpackState()
        {
            _backpackPanel.style.display = _backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshInventory()
        {
            _inventory.Refresh();
            RefreshBackpackCount();
            InvalidateShownStats();
        }

        private void RefreshBackpackCount()
        {
            int used = 0;

            foreach (ItemData item in _equipment.Backpack)
            {
                if (item != null)
                    used++;
            }

            _backpackToggle.text = $"{used}/{_equipment.BackpackCapacity}";
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

            if (_hpFill != null)
                _hpFill.style.width = Length.Percent(_player.HealthRatio * 100f);

            if (Mathf.Approximately(_shownHealth, _player.CurrentHealth)
                && Mathf.Approximately(_shownMaxHealth, _player.MaxHealth)
                && Mathf.Approximately(_shownDamage, stats.Damage))
                return;

            _shownHealth = _player.CurrentHealth;
            _shownMaxHealth = _player.MaxHealth;
            _shownDamage = stats.Damage;

            _hpText.text = _localization.Get(LocalizationKeys.Arpg.StatHealth,
                _player.CurrentHealth.ToString("F0"), _player.MaxHealth.ToString("F0"));
            _damage.text = _localization.Get(LocalizationKeys.Arpg.StatDamage,
                stats.Damage.ToString("F0"), stats.DamageType);
            _physical.text = _localization.Get(LocalizationKeys.Arpg.StatPhysical, stats.PhysicalResist.ToString("P0"));
            _magic.text = _localization.Get(LocalizationKeys.Arpg.StatMagic, stats.MagicResist.ToString("P0"));
            _fire.text = _localization.Get(LocalizationKeys.Arpg.StatFire, stats.FireResist.ToString("P0"));
        }

        private void RefreshGold()
        {
            if (_goldValue != null)
                _goldValue.text = _progress.Gold.ToString();
        }

        private void RefreshExp()
        {
            if (_expValue != null)
                _expValue.text = _progress.Exp.ToString();
        }
    }
}
