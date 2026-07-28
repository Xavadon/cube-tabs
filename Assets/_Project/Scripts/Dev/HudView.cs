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
        private const string OpenToggleClass = "backpack-toggle--open";

        private IPlayerProgressService _progress;
        private IEquipmentService _equipment;
        private ILocalizationService _localization;
        private IUnitPreviewService _preview;
        private CharacterEntity _player;
        private InventoryPanel _inventory;

        private Label _goldValue;
        private VisualElement _expFill;
        private Label _levelValue;
        private VisualElement _hpFill;
        private Label _hpText;
        private Label _hpRegen;
        private Label _damage;
        private Label _armor;
        private Label _magic;
        private Label _attackSpeed;
        private Label _attributes;
        private VisualElement _backpackPanel;
        private Button _backpackToggle;

        private bool _backpackOpen;
        private int _shownHealth = -1;
        private int _shownMaxHealth = -1;
        private int _shownDamage = -1;

        public event Action OnMerchantClicked;

        // ставится снаружи и переживает пересборку дерева UIDocument: панель создаётся заново в Wire
        public Func<ItemData, bool> ItemClickInterceptor { get; set; }

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
            _progress.OnLevelChanged += RefreshExp;
            _equipment.OnChanged += RefreshInventory;

            StartWiring();
        }

        protected override void Wire()
        {
            _goldValue = Root.Q<Label>("gold-value");
            _expFill = Root.Q<VisualElement>("exp-fill");
            _levelValue = Root.Q<Label>("level-value");
            _hpFill = Root.Q<VisualElement>("hp-fill");
            _hpText = Root.Q<Label>("hp-text");
            _hpRegen = Root.Q<Label>("hp-regen");
            _damage = Root.Q<Label>("stat-damage");
            _armor = Root.Q<Label>("stat-armor");
            _magic = Root.Q<Label>("stat-magic");
            _attackSpeed = Root.Q<Label>("stat-attack-speed");
            _attributes = Root.Q<Label>("stat-attributes");
            _backpackPanel = Root.Q<VisualElement>("backpack-panel");
            _backpackToggle = Root.Q<Button>("backpack-toggle");

            Root.Q<Label>("backpack-title").text = _localization.Get(LocalizationKeys.Arpg.BackpackSection);

            var merchantButton = Root.Q<Button>("shop-button");
            merchantButton.text = _localization.Get(LocalizationKeys.Arpg.HudMerchant);
            merchantButton.clicked += () => OnMerchantClicked?.Invoke();

            _backpackToggle.clicked += ToggleBackpack;

            _inventory = new InventoryPanel(_equipment, _localization, Root,
                Root.Q<VisualElement>("equipment"), Root.Q<VisualElement>("backpack"))
            {
                ClickInterceptor = item => ItemClickInterceptor != null && ItemClickInterceptor(item)
            };

            ApplyBackpackState();
            ApplyPortrait();
            InvalidateShownStats();
            RefreshGold();
            RefreshExp();
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
                _progress.OnLevelChanged -= RefreshExp;
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

            if (_backpackOpen)
                _backpackToggle.AddToClassList(OpenToggleClass);
            else
                _backpackToggle.RemoveFromClassList(OpenToggleClass);
        }

        private void RefreshInventory()
        {
            _inventory.Refresh();
            InvalidateShownStats();
        }

        private void InvalidateShownStats()
        {
            _shownHealth = -1;
            _shownMaxHealth = -1;
            _shownDamage = -1;
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

            // сравниваем округлённые значения: реген тикает каждый кадр,
            // иначе строки пересобирались бы постоянно
            int health = Mathf.CeilToInt(_player.CurrentHealth);
            int maxHealth = Mathf.RoundToInt(_player.MaxHealth);
            int damage = Mathf.RoundToInt(stats.Damage);

            if (health == _shownHealth && maxHealth == _shownMaxHealth && damage == _shownDamage)
                return;

            _shownHealth = health;
            _shownMaxHealth = maxHealth;
            _shownDamage = damage;

            _hpText.text = _localization.Get(LocalizationKeys.Arpg.StatHealthShort, health, maxHealth);
            _hpRegen.text = _localization.Get(LocalizationKeys.Arpg.StatHealthRegen, stats.HealthRegen.ToString("0.#"));
            _damage.text = _localization.Get(LocalizationKeys.Arpg.StatDamage,
                stats.Damage.ToString("F0"), stats.DamageType);
            _armor.text = _localization.Get(LocalizationKeys.Arpg.StatArmor, stats.Armor.ToString("0.#"));
            _magic.text = _localization.Get(LocalizationKeys.Arpg.StatMagic, stats.MagicResist.ToString("P1"));
            _attackSpeed.text = _localization.Get(LocalizationKeys.Arpg.StatAttackSpeed, stats.AttackSpeed.ToString("F0"));
            _attributes.text = $"{stats.Strength:F0} / {stats.Agility:F0} / {stats.Intelligence:F0}";
        }

        private void RefreshGold()
        {
            if (_goldValue != null)
                _goldValue.text = _progress.Gold.ToString();
        }

        private void RefreshExp()
        {
            if (_expFill != null)
                _expFill.style.width = Length.Percent(_progress.ExpProgress * 100f);

            if (_levelValue != null)
                _levelValue.text = _localization.Get(LocalizationKeys.Arpg.Level, _progress.Level);
        }
    }
}
