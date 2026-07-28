using System.Text;
using _Project.Scripts.Architecture.Services.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Inventory.UI
{
    public class ItemTooltip
    {
        private const float Offset = 12f;

        private readonly ILocalizationService _localization;
        private readonly VisualElement _root;
        private readonly VisualElement _tooltip;
        private readonly Label _title;
        private readonly Label _body;

        public ItemTooltip(ILocalizationService localization, VisualElement root)
        {
            _localization = localization;
            _root = root;

            _tooltip = root.Q<VisualElement>("tooltip");
            _title = root.Q<Label>("tooltip-title");
            _body = root.Q<Label>("tooltip-body");
        }

        public void Show(ItemData item, VisualElement anchor, string footer = null)
        {
            if (item == null || anchor == null || _tooltip == null)
                return;

            string bonus = DescribeBonus(item.Bonus);

            _title.text = item.DisplayName;
            _body.text = string.IsNullOrEmpty(footer) ? bonus : bonus + "\n" + footer;
            _tooltip.style.display = DisplayStyle.Flex;

            Rect bounds = anchor.worldBound;
            float left = Mathf.Min(bounds.xMax + Offset, _root.worldBound.width - _tooltip.resolvedStyle.width);
            _tooltip.style.left = Mathf.Max(0f, left);
            _tooltip.style.top = Mathf.Max(0f, bounds.yMin - _tooltip.resolvedStyle.height - Offset);
        }

        public void Hide()
        {
            if (_tooltip != null)
                _tooltip.style.display = DisplayStyle.None;
        }

        private string DescribeBonus(StatBonus bonus)
        {
            if (bonus == null)
                return _localization.Get(LocalizationKeys.Arpg.BonusNone);

            var builder = new StringBuilder();

            AppendBonus(builder, LocalizationKeys.Arpg.BonusStrength, bonus.Strength, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusAgility, bonus.Agility, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusIntelligence, bonus.Intelligence, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusDamage, bonus.Damage, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusHealth, bonus.Health, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusArmor, bonus.Armor, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusAttackSpeed, bonus.AttackSpeed, false);
            AppendBonus(builder, LocalizationKeys.Arpg.BonusMagic, bonus.MagicResist, true);

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
    }
}
