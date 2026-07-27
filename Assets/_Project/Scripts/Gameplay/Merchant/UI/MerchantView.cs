using System;
using System.Text;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Merchant.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Merchant.UI
{
    public class MerchantView : UIDocumentView
    {
        private IMerchantService _merchant;
        private IPlayerProgressService _progress;
        private ILocalizationService _localization;

        private VisualElement _stock;
        private VisualElement _recipes;
        private Label _gold;
        private bool _open;

        public bool IsOpen => _open;

        public void Bind(IMerchantService merchant, IPlayerProgressService progress, ILocalizationService localization)
        {
            _merchant = merchant;
            _progress = progress;
            _localization = localization;

            _merchant.OnChanged += Refresh;
            _progress.OnGoldChanged += Refresh;

            StartWiring();
        }

        protected override void Wire()
        {
            _stock = Root.Q<VisualElement>("stock");
            _recipes = Root.Q<VisualElement>("recipes");
            _gold = Root.Q<Label>("gold");

            Root.Q<Label>("title").text = _localization.Get(LocalizationKeys.Arpg.MerchantTitle);
            Root.Q<Label>("stock-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantGoods);
            Root.Q<Label>("recipes-title").text = _localization.Get(LocalizationKeys.Arpg.MerchantCraft);
            Root.Q<Button>("close").clicked += Close;

            ApplyOpenState();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_merchant != null)
                _merchant.OnChanged -= Refresh;

            if (_progress != null)
                _progress.OnGoldChanged -= Refresh;
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

            RefreshStock();
            RefreshRecipes();
        }

        private void RefreshStock()
        {
            _stock.Clear();

            foreach (MerchantEntry entry in _merchant.Stock)
            {
                if (entry?.Item == null)
                    continue;

                MerchantEntry captured = entry;
                _stock.Add(BuildRow(
                    ItemName(entry.Item),
                    _localization.Get(LocalizationKeys.Arpg.MerchantPrice, entry.Price),
                    _merchant.CanBuy(entry),
                    () => _merchant.Buy(captured)));
            }
        }

        private void RefreshRecipes()
        {
            _recipes.Clear();

            foreach (RecipeData recipe in _merchant.Recipes)
            {
                if (recipe?.Result == null)
                    continue;

                RecipeData captured = recipe;
                _recipes.Add(BuildRow(
                    $"{ItemName(recipe.Result)}\n<size=14>{Ingredients(recipe)}</size>",
                    _localization.Get(LocalizationKeys.Arpg.MerchantCraftButton),
                    _merchant.CanCraft(recipe),
                    () => _merchant.Craft(captured)));
            }
        }

        private VisualElement BuildRow(string text, string buttonText, bool enabled, Action action)
        {
            var row = new VisualElement();
            row.AddToClassList("row");

            var label = new Label(text) { enableRichText = true };
            label.AddToClassList("row-text");
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);

            var button = new Button(() => action()) { text = buttonText };
            button.AddToClassList("row-button");
            button.SetEnabled(enabled);
            row.Add(button);

            return row;
        }

        private string Ingredients(RecipeData recipe)
        {
            if (recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                return _localization.Get(LocalizationKeys.Arpg.MerchantNoComponents);

            var builder = new StringBuilder();

            foreach (ItemData ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(" + ");

                builder.Append(ItemName(ingredient));
            }

            return builder.ToString();
        }

        private static string ItemName(ItemData item)
        {
            return string.IsNullOrEmpty(item.Name) ? item.name : item.Name;
        }
    }
}
