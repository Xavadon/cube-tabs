using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Merchant.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Merchant
{
    public interface IMerchantService : IService
    {
        IReadOnlyList<MerchantEntry> Stock { get; }
        IReadOnlyList<ItemData> Craftables { get; }

        RecipeData RecipeFor(ItemData result);
        IReadOnlyList<RecipeData> RecipesUsing(ItemData ingredient);
        int PriceOf(ItemData item);
        bool CanBuy(ItemData item);
        bool Buy(ItemData item);

        event Action OnChanged;
    }

    public class MerchantService : IMerchantService
    {
        private const string StockPath = "Data/ArpgMerchantStock";
        private const string RecipeBookPath = "Data/ArpgRecipeBook";
        private const int MaxCascade = 16;
        private const int MaxRecipeDepth = 8;

        private static readonly MerchantEntry[] EmptyStock = Array.Empty<MerchantEntry>();
        private static readonly RecipeData[] EmptyRecipes = Array.Empty<RecipeData>();

        private readonly IPlayerProgressService _progress;
        private readonly IEquipmentService _equipment;

        private readonly Dictionary<ItemData, MerchantEntry> _entries = new();
        private readonly Dictionary<ItemData, RecipeData> _recipeByResult = new();
        private readonly Dictionary<ItemData, List<RecipeData>> _recipesByIngredient = new();
        private readonly List<ItemData> _craftables = new();

        private MerchantEntry[] _stock = EmptyStock;
        private RecipeData[] _recipes = EmptyRecipes;
        private bool _combining;

        public IReadOnlyList<MerchantEntry> Stock => _stock;
        public IReadOnlyList<ItemData> Craftables => _craftables;

        public event Action OnChanged;

        public MerchantService(IPlayerProgressService progress, IEquipmentService equipment)
        {
            _progress = progress;
            _equipment = equipment;
        }

        public UniTask Initialize()
        {
            var stock = Resources.Load<MerchantStockData>(StockPath);
            if (stock != null && stock.Entries != null)
                _stock = stock.Entries;

            var book = Resources.Load<RecipeBookData>(RecipeBookPath);
            if (book != null && book.Recipes != null)
                _recipes = book.Recipes;

            BuildIndex();
            ValidateRecipes();

            _equipment.OnChanged += HandleInventoryChanged;

            Debug.Log($"[MerchantService] Initialized: {_stock.Length} товаров, {_recipes.Length} рецептов");
            return UniTask.CompletedTask;
        }

        public RecipeData RecipeFor(ItemData result)
        {
            return result != null && _recipeByResult.TryGetValue(result, out RecipeData recipe) ? recipe : null;
        }

        public IReadOnlyList<RecipeData> RecipesUsing(ItemData ingredient)
        {
            return ingredient != null && _recipesByIngredient.TryGetValue(ingredient, out List<RecipeData> recipes)
                ? recipes
                : EmptyRecipes;
        }

        public int PriceOf(ItemData item)
        {
            var leaves = new List<ItemData>();
            return CollectLeaves(item, leaves, 0) ? TotalPrice(leaves) : 0;
        }

        public bool CanBuy(ItemData item)
        {
            var leaves = new List<ItemData>();

            if (!CollectLeaves(item, leaves, 0))
                return false;

            return _progress.CanAfford(TotalPrice(leaves)) && FreeCells() >= leaves.Count;
        }

        // предмет из ассортимента покупается как есть, собираемый — своими компонентами,
        // рекурсивно до тех, что реально продаются
        public bool Buy(ItemData item)
        {
            var leaves = new List<ItemData>();

            if (!CollectLeaves(item, leaves, 0))
                return false;

            int price = TotalPrice(leaves);

            if (!_progress.CanAfford(price) || FreeCells() < leaves.Count)
                return false;

            _progress.SpendGold(price);

            foreach (ItemData leaf in leaves)
                _equipment.AddToBackpack(leaf);

            OnChanged?.Invoke();
            return true;
        }

        private void BuildIndex()
        {
            foreach (MerchantEntry entry in _stock)
            {
                if (entry?.Item != null)
                    _entries[entry.Item] = entry;
            }

            foreach (RecipeData recipe in _recipes)
            {
                if (recipe?.Result == null || recipe.Ingredients == null)
                    continue;

                _recipeByResult[recipe.Result] = recipe;

                foreach (ItemData ingredient in recipe.Ingredients)
                {
                    if (ingredient == null)
                        continue;

                    if (!_recipesByIngredient.TryGetValue(ingredient, out List<RecipeData> recipes))
                    {
                        recipes = new List<RecipeData>();
                        _recipesByIngredient[ingredient] = recipes;
                    }

                    if (!recipes.Contains(recipe))
                        recipes.Add(recipe);
                }
            }

            // витрина сборного: результаты рецептов, которые напрямую не продаются
            foreach (RecipeData recipe in _recipes)
            {
                if (recipe?.Result == null || _entries.ContainsKey(recipe.Result) || _craftables.Contains(recipe.Result))
                    continue;

                _craftables.Add(recipe.Result);
            }
        }

        // сборка берёт первый подходящий рецепт по порядку в книге, поэтому рецепт,
        // чьи ингредиенты — подмножество другого, навсегда закрывает тот другой
        private void ValidateRecipes()
        {
            for (int i = 0; i < _recipes.Length; i++)
            {
                for (int j = 0; j < _recipes.Length; j++)
                {
                    if (i == j || !IsValid(_recipes[i]) || !IsValid(_recipes[j]))
                        continue;

                    if (i < j && Covers(_recipes[j], _recipes[i]))
                    {
                        Debug.LogError($"[MerchantService] Рецепт '{_recipes[i].Result.DisplayName}' перекрывает " +
                                       $"'{_recipes[j].Result.DisplayName}': его ингредиенты входят в набор второго, " +
                                       "и второй никогда не соберётся");
                    }
                }
            }
        }

        private static bool IsValid(RecipeData recipe)
        {
            return recipe?.Result != null && recipe.Ingredients != null && recipe.Ingredients.Length > 0;
        }

        private static bool Covers(RecipeData outer, RecipeData inner)
        {
            var pool = new List<ItemData>(outer.Ingredients);

            foreach (ItemData ingredient in inner.Ingredients)
            {
                if (!pool.Remove(ingredient))
                    return false;
            }

            return true;
        }

        private bool CollectLeaves(ItemData item, List<ItemData> leaves, int depth)
        {
            if (item == null || depth > MaxRecipeDepth)
                return false;

            if (_entries.ContainsKey(item))
            {
                leaves.Add(item);
                return true;
            }

            RecipeData recipe = RecipeFor(item);

            if (recipe?.Ingredients == null || recipe.Ingredients.Length == 0)
                return false;

            foreach (ItemData ingredient in recipe.Ingredients)
            {
                if (!CollectLeaves(ingredient, leaves, depth + 1))
                    return false;
            }

            return true;
        }

        private int TotalPrice(List<ItemData> leaves)
        {
            int total = 0;

            foreach (ItemData leaf in leaves)
                total += _entries[leaf].Price;

            return total;
        }

        private int FreeCells()
        {
            int free = 0;

            for (int i = 0; i < _equipment.BackpackCapacity; i++)
            {
                if (_equipment.Backpack[i] == null)
                    free++;
            }

            return free;
        }

        // предметы склеиваются только в зоне крафта: рюкзак и надетое трогать нельзя,
        // иначе игрок теряет компоненты, которые копил
        private void HandleInventoryChanged()
        {
            if (_combining)
                return;

            _combining = true;

            for (int i = 0; i < MaxCascade && TryCombine(); i++)
            {
            }

            _combining = false;
        }

        private bool TryCombine()
        {
            foreach (RecipeData recipe in _recipes)
            {
                if (recipe?.Result == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                    continue;

                List<int> cells = MatchCraftCells(recipe);
                if (cells == null)
                    continue;

                foreach (int cell in cells)
                    _equipment.SetCraftCell(cell, null);

                _equipment.SetCraftCell(cells[0], recipe.Result);

                OnChanged?.Invoke();
                return true;
            }

            return false;
        }

        private List<int> MatchCraftCells(RecipeData recipe)
        {
            var used = new List<int>();

            foreach (ItemData ingredient in recipe.Ingredients)
            {
                if (ingredient == null)
                    return null;

                int cell = FindCraftCell(ingredient, used);

                if (cell < 0)
                    return null;

                used.Add(cell);
            }

            return used;
        }

        private int FindCraftCell(ItemData ingredient, List<int> used)
        {
            for (int i = 0; i < _equipment.CraftCapacity; i++)
            {
                if (_equipment.CraftZone[i] == ingredient && !used.Contains(i))
                    return i;
            }

            return -1;
        }
    }
}
