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
        IReadOnlyList<RecipeData> Recipes { get; }

        bool CanBuy(MerchantEntry entry);
        bool Buy(MerchantEntry entry);
        bool CanCraft(RecipeData recipe);
        bool Craft(RecipeData recipe);

        event Action OnChanged;
    }

    public class MerchantService : IMerchantService
    {
        private const string StockPath = "Data/ArpgMerchantStock";
        private const string RecipeBookPath = "Data/ArpgRecipeBook";

        private static readonly MerchantEntry[] EmptyStock = Array.Empty<MerchantEntry>();
        private static readonly RecipeData[] EmptyRecipes = Array.Empty<RecipeData>();

        private readonly IPlayerProgressService _progress;
        private readonly IEquipmentService _equipment;

        private MerchantEntry[] _stock = EmptyStock;
        private RecipeData[] _recipes = EmptyRecipes;

        public IReadOnlyList<MerchantEntry> Stock => _stock;
        public IReadOnlyList<RecipeData> Recipes => _recipes;

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

            Debug.Log($"[MerchantService] Initialized: {_stock.Length} товаров, {_recipes.Length} рецептов");
            return UniTask.CompletedTask;
        }

        public bool CanBuy(MerchantEntry entry)
        {
            return entry != null && entry.Item != null && _progress.CanAfford(entry.Price);
        }

        public bool Buy(MerchantEntry entry)
        {
            if (!CanBuy(entry))
                return false;

            if (!_equipment.HasFreeCell)
                return false;

            _progress.SpendGold(entry.Price);
            _equipment.AddToBackpack(entry.Item);

            OnChanged?.Invoke();
            return true;
        }

        public bool CanCraft(RecipeData recipe)
        {
            if (recipe == null || recipe.Result == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                return false;

            var pool = new List<ItemData>();

            foreach (ItemData cell in _equipment.Backpack)
            {
                if (cell != null)
                    pool.Add(cell);
            }

            foreach (ItemData ingredient in recipe.Ingredients)
            {
                if (ingredient == null || !pool.Remove(ingredient))
                    return false;
            }

            return true;
        }

        public bool Craft(RecipeData recipe)
        {
            if (!CanCraft(recipe))
                return false;

            foreach (ItemData ingredient in recipe.Ingredients)
                _equipment.RemoveFromBackpack(ingredient);

            _equipment.AddToBackpack(recipe.Result);

            OnChanged?.Invoke();
            return true;
        }
    }
}
