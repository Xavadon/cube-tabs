using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Merchant.Data;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.Merchant.UI
{
    public class RecipeTreeView
    {
        private static readonly (VisualElement, VisualElement)[] NoLinks =
            Array.Empty<(VisualElement, VisualElement)>();

        private readonly IMerchantService _merchant;
        private readonly MerchantSlotFactory _factory;
        private readonly ILocalizationService _localization;
        private readonly VisualElement _container;
        private readonly RecipeLinks _links = new();
        private readonly List<MerchantSlot> _slots = new();

        private ItemData _selected;

        public RecipeTreeView(IMerchantService merchant, MerchantSlotFactory factory,
            ILocalizationService localization, VisualElement container)
        {
            _merchant = merchant;
            _factory = factory;
            _localization = localization;
            _container = container;

            _container.Add(_links);

            // позиции иконок известны только после раскладки, поэтому линии перерисовываются по геометрии
            _container.RegisterCallback<GeometryChangedEvent>(_ => _links.MarkDirtyRepaint());

            Rebuild();
        }

        public void Show(ItemData item)
        {
            _selected = item;
            Rebuild();
        }

        public void RefreshAffordable()
        {
            _factory.RefreshAffordable(_slots);
        }

        private void Rebuild()
        {
            for (int i = _container.childCount - 1; i >= 0; i--)
            {
                if (_container[i] != _links)
                    _container.RemoveAt(i);
            }

            _slots.Clear();

            if (_selected == null)
            {
                _container.Add(BuildHint());
                _links.Set(NoLinks);
                return;
            }

            IReadOnlyList<RecipeData> parents = _merchant.RecipesUsing(_selected);
            RecipeData recipe = _merchant.RecipeFor(_selected);

            List<MerchantSlot> parentSlots = BuildParentsRow(parents);
            MerchantSlot current = BuildCurrentRow();
            List<MerchantSlot> ingredientSlots = BuildIngredientsRow(recipe);

            var links = new List<(VisualElement, VisualElement)>();

            foreach (MerchantSlot parent in parentSlots)
                links.Add((current.Element, parent.Element));

            foreach (MerchantSlot ingredient in ingredientSlots)
                links.Add((current.Element, ingredient.Element));

            _links.Set(links);
            _links.SendToBack();
        }

        private List<MerchantSlot> BuildParentsRow(IReadOnlyList<RecipeData> parents)
        {
            var slots = new List<MerchantSlot>();

            if (parents.Count == 0)
                return slots;

            VisualElement row = AddRow("tree-row--parents");

            foreach (RecipeData recipe in parents)
            {
                if (recipe?.Result == null)
                    continue;

                MerchantSlot slot = AddSlot(row, recipe.Result, false);
                slots.Add(slot);
            }

            return slots;
        }

        private MerchantSlot BuildCurrentRow()
        {
            VisualElement row = AddRow("tree-row--current");
            return AddSlot(row, _selected, true);
        }

        private List<MerchantSlot> BuildIngredientsRow(RecipeData recipe)
        {
            var slots = new List<MerchantSlot>();

            if (recipe?.Ingredients == null || recipe.Ingredients.Length == 0)
                return slots;

            VisualElement row = AddRow("tree-row--ingredients");

            foreach (ItemData ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                    continue;

                slots.Add(AddSlot(row, ingredient, false));
            }

            return slots;
        }

        private VisualElement AddRow(string modifier)
        {
            var row = new VisualElement();
            row.AddToClassList("tree-row");
            row.AddToClassList(modifier);
            _container.Add(row);

            return row;
        }

        private MerchantSlot AddSlot(VisualElement row, ItemData item, bool selected)
        {
            MerchantSlot slot = _factory.Create(item, Show, selected);

            row.Add(slot.Element);
            _slots.Add(slot);

            return slot;
        }

        private Label BuildHint()
        {
            var hint = new Label(_localization.Get(LocalizationKeys.Arpg.MerchantTreeHint));
            hint.AddToClassList("tree-hint");
            hint.pickingMode = PickingMode.Ignore;

            return hint;
        }
    }
}
