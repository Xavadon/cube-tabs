using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Components.Abilities;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
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
        private const string EmptyAbilityClass = "ability-button--empty";

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
        private VisualElement _manaFill;
        private Label _manaText;
        private VisualElement _abilityRow;
        private VisualElement _bossPanel;
        private VisualElement _bossFill;
        private Label _bossName;
        private Label _bossHp;
        private CharacterEntity _boss;
        private int _shownBossHealth = -1;
        private Label _damage;
        private Label _armor;
        private Label _magic;
        private Label _attackSpeed;
        private Label _attributes;
        private VisualElement _backpackPanel;
        private Button _backpackToggle;

        private readonly List<AbilityButton> _abilityButtons = new();

        private bool _backpackOpen;
        private int _shownHealth = -1;
        private int _shownMaxHealth = -1;
        private int _shownDamage = -1;
        private int _shownMana = -1;

        private class AbilityButton
        {
            public int Index;
            public VisualElement Cooldown;
            public Label Timer;
            public VisualElement Root;
            public int ShownSeconds = -1;
            public bool ShownEmpty;
        }

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
            _manaFill = Root.Q<VisualElement>("mana-fill");
            _manaText = Root.Q<Label>("mana-text");
            _abilityRow = Root.Q<VisualElement>("abilities");
            _bossPanel = Root.Q<VisualElement>("boss-panel");
            _bossFill = Root.Q<VisualElement>("boss-fill");
            _bossName = Root.Q<Label>("boss-name");
            _bossHp = Root.Q<Label>("boss-hp");
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

            BuildAbilities();
            ApplyBackpackState();
            ApplyPortrait();
            InvalidateShownStats();
            RefreshGold();
            RefreshExp();
        }

        public void ShowBoss(CharacterEntity boss)
        {
            _boss = boss;
            _shownBossHealth = -1;

            if (_bossName != null && boss != null && boss.CharacterDataRef != null)
                _bossName.text = boss.CharacterDataRef.Name;

            ApplyBossState();
        }

        private void ApplyBossState()
        {
            if (_bossPanel != null)
                _bossPanel.style.display = _boss != null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // босс уничтожается вместе с объектом, поэтому панель снимается по проверке на null
        private void RefreshBoss()
        {
            if (_boss == null)
            {
                if (_bossPanel != null && _bossPanel.style.display == DisplayStyle.Flex)
                    ApplyBossState();

                return;
            }

            _bossFill.style.width = Length.Percent(_boss.HealthRatio * 100f);

            int health = Mathf.CeilToInt(_boss.CurrentHealth);

            if (health == _shownBossHealth)
                return;

            _shownBossHealth = health;
            _bossHp.text = $"{health}/{Mathf.RoundToInt(_boss.MaxHealth)}";
        }

        private void BuildAbilities()
        {
            _abilityRow.Clear();
            _abilityButtons.Clear();

            AbilityCaster caster = _player != null ? _player.Abilities : null;

            if (caster == null)
            {
                _abilityRow.style.display = DisplayStyle.None;
                return;
            }

            _abilityRow.style.display = DisplayStyle.Flex;

            for (int i = 0; i < caster.Slots.Count; i++)
            {
                AbilitySlotData slot = caster.Slots[i];

                if (slot?.Ability == null)
                    continue;

                _abilityButtons.Add(BuildAbilityButton(caster, slot, i));
            }
        }

        private AbilityButton BuildAbilityButton(AbilityCaster caster, AbilitySlotData slot, int index)
        {
            int captured = index;
            var button = new Button(() => caster.TryCast(captured));
            button.AddToClassList("ability-button");

            var icon = new VisualElement();
            icon.AddToClassList("ability-icon");
            icon.pickingMode = PickingMode.Ignore;

            if (slot.Icon != null)
                icon.style.backgroundImage = Background.FromSprite(slot.Icon);

            var cooldown = new VisualElement();
            cooldown.AddToClassList("ability-cooldown");
            cooldown.pickingMode = PickingMode.Ignore;

            var timer = new Label();
            timer.AddToClassList("ability-timer");
            timer.pickingMode = PickingMode.Ignore;

            button.Add(icon);
            button.Add(cooldown);
            button.Add(timer);
            _abilityRow.Add(button);

            return new AbilityButton { Index = index, Root = button, Cooldown = cooldown, Timer = timer };
        }

        private void RefreshAbilities()
        {
            AbilityCaster caster = _player.Abilities;

            if (caster == null)
                return;

            foreach (AbilityButton button in _abilityButtons)
            {
                float left = caster.CooldownLeft(button.Index);
                int seconds = Mathf.CeilToInt(left);

                if (seconds != button.ShownSeconds)
                {
                    button.ShownSeconds = seconds;
                    button.Timer.text = seconds > 0 ? seconds.ToString() : string.Empty;
                    button.Cooldown.style.height = Length.Percent(caster.CooldownRatio(button.Index) * 100f);
                }

                bool empty = !caster.CanCast(button.Index) && left <= 0f;

                if (empty == button.ShownEmpty)
                    continue;

                button.ShownEmpty = empty;

                if (empty)
                    button.Root.AddToClassList(EmptyAbilityClass);
                else
                    button.Root.RemoveFromClassList(EmptyAbilityClass);
            }
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

            RefreshMana();
            RefreshAbilities();
            RefreshBoss();

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

        private void RefreshMana()
        {
            if (_manaFill != null)
                _manaFill.style.width = Length.Percent(_player.ManaRatio * 100f);

            int mana = Mathf.FloorToInt(_player.CurrentMana);

            if (mana == _shownMana || _manaText == null)
                return;

            _shownMana = mana;
            _manaText.text = $"{mana}/{Mathf.RoundToInt(_player.MaxMana)}";
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
