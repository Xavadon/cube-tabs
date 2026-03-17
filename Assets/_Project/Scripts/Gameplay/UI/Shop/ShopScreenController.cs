using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopScreenController : IDisposable
    {
        private readonly IShopScreenView _view;
        private readonly IPlayerProgressService _progress;
        private readonly IPurchaseService _purchaseService;
        private readonly ShopCatalog _catalog;
        private readonly UnitPreviewFactory _portraitFactory;
        private readonly UnitPreviewFactory _fullBodyFactory;
        private readonly Dictionary<int, RenderTexture> _portraitCache;
        private readonly Dictionary<int, PreviewHandle> _fullBodyCache = new();
        private readonly List<CardEntry> _cardEntries = new();

        private Selection _selection;
        private bool _dirty;

        public ShopScreenController(
            IShopScreenView view,
            IPlayerProgressService progress,
            IPurchaseService purchaseService,
            ShopCatalog catalog,
            UnitPreviewFactory portraitFactory,
            UnitPreviewFactory fullBodyFactory,
            Dictionary<int, RenderTexture> portraitCache)
        {
            _view = view;
            _progress = progress;
            _purchaseService = purchaseService;
            _catalog = catalog;
            _portraitFactory = portraitFactory;
            _fullBodyFactory = fullBodyFactory;
            _portraitCache = portraitCache;

            _view.CardClicked += OnCardClicked;
            _view.BuyClicked += OnBuyClicked;
            _view.ViewEnabled += OnViewEnabled;

            _progress.OnGoldChanged += ScheduleRebuild;
            _progress.OnOwnedChanged += ScheduleRebuild;
            _progress.OnArmyChanged += ScheduleRebuild;

            Rebuild();
        }

        public void OnLateUpdate()
        {
            if (!_dirty)
                return;

            _dirty = false;
            Rebuild();
        }

        public void Dispose()
        {
            _portraitFactory?.Dispose();
            _fullBodyFactory?.Dispose();

            _view.CardClicked -= OnCardClicked;
            _view.BuyClicked -= OnBuyClicked;
            _view.ViewEnabled -= OnViewEnabled;

            _progress.OnGoldChanged -= ScheduleRebuild;
            _progress.OnOwnedChanged -= ScheduleRebuild;
            _progress.OnArmyChanged -= ScheduleRebuild;
        }

        private void ScheduleRebuild() => _dirty = true;

        private void OnViewEnabled() => Rebuild();

        private void OnBuyClicked()
        {
            if (!_selection.HasValue)
                return;

            if (_selection.IsHero)
            {
                _progress.BuyUniqueUnit(_selection.HeroData);
            }
            else
            {
                _purchaseService.Purchase(_selection.ItemData,
                    onSuccess: () => _progress.GrantItemReward(_selection.ItemData),
                    onFailure: null);
            }
        }

        private void OnCardClicked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardEntries.Count)
                return;

            var entry = _cardEntries[cardIndex];

            _selection = entry.IsHero
                ? Selection.Hero(entry.HeroData)
                : Selection.Item(entry.ItemData);

            for (int i = 0; i < _cardEntries.Count; i++)
                _view.SetCardSelected(i, i == cardIndex);

            RefreshPreview();
            RefreshBuyButton();
        }

        private void Rebuild()
        {
            _view.ClearCards();
            _cardEntries.Clear();

            SpawnHeroCards();
            SpawnItemCards();

            RestoreSelection();
            RefreshPreview();
            RefreshBuyButton();
        }

        private void SpawnHeroCards()
        {
            if (_catalog.UniqueHeroes == null)
                return;

            foreach (var hero in _catalog.UniqueHeroes)
            {
                var portrait = GetOrCreatePortrait(hero, 0);
                var tier = hero.GetTier(0);
                float hp = tier.Stats.Health;

                float damage;

                if (tier.WeaponData is { Length: > 0 } && tier.WeaponData[0] != null)
                    damage = tier.WeaponData[0].Damage;
                else if (tier.Ability != null)
                    damage = tier.Ability.Damage;
                else
                    damage = 0f;

                float speed = tier.MoveSpeed;

                _view.AddHeroCard(hero.Name, portrait, hp, damage, speed);
                _cardEntries.Add(CardEntry.ForHero(hero));
            }
        }

        private void SpawnItemCards()
        {
            if (_catalog.ShopItems == null)
                return;

            foreach (var item in _catalog.ShopItems)
            {
                _view.AddItemCard(item.Name, item.Icon);
                _cardEntries.Add(CardEntry.ForItem(item));
            }
        }

        private void RestoreSelection()
        {
            if (!_selection.HasValue)
                return;

            bool found = false;

            for (int i = 0; i < _cardEntries.Count; i++)
            {
                bool match = _selection.IsHero
                    ? _cardEntries[i].IsHero && _cardEntries[i].HeroData == _selection.HeroData
                    : !_cardEntries[i].IsHero && _cardEntries[i].ItemData == _selection.ItemData;

                _view.SetCardSelected(i, match);

                if (match)
                    found = true;
            }

            if (!found)
                _selection.Clear();
        }

        private void RefreshPreview()
        {
            if (!_selection.HasValue)
            {
                _view.HidePreview();
                return;
            }

            if (_selection.IsHero)
            {
                var handle = GetOrCreateFullBody(_selection.HeroData, 0);
                _view.ShowUnitPreview(handle);
            }
            else
            {
                if (_selection.ItemData.Icon != null)
                    _view.ShowItemPreview(_selection.ItemData.Icon);
                else
                    _view.HidePreview();
            }
        }

        private void RefreshBuyButton()
        {
            if (!_selection.HasValue)
            {
                _view.SetBuyVisible(false);
                return;
            }

            _view.SetBuyVisible(true);

            if (_selection.IsHero)
            {
                bool owned = _progress.IsUnitOwned(_selection.HeroData.Id);

                if (owned)
                {
                    _view.SetBuyLabel("Куплено");
                    _view.SetBuyInteractable(false);
                }
                else
                {
                    _view.SetBuyLabel($"Купить ({_selection.HeroData.Price})");
                    _view.SetBuyInteractable(_progress.CanAfford(_selection.HeroData.Price));
                }
            }
            else
            {
                _view.SetBuyLabel($"Купить ({_selection.ItemData.PriceLabel})");
                _view.SetBuyInteractable(true);
            }
        }

        private RenderTexture GetOrCreatePortrait(CharacterData data, int tierIndex)
        {
            int key = data.Id * 100 + tierIndex;

            if (_portraitCache.TryGetValue(key, out var existing))
                return existing;

            var handle = _portraitFactory.CreatePreview(data, tierIndex, _portraitCache.Count);
            _portraitCache[key] = handle.Texture;
            return handle.Texture;
        }

        private PreviewHandle GetOrCreateFullBody(CharacterData data, int tierIndex)
        {
            int key = data.Id * 100 + tierIndex;

            if (_fullBodyCache.TryGetValue(key, out var existing))
                return existing;

            var handle = _fullBodyFactory.CreatePreview(data, tierIndex, _fullBodyCache.Count);
            _fullBodyCache[key] = handle;
            return handle;
        }

        private struct Selection
        {
            public CharacterData HeroData;
            public ShopItemData ItemData;

            public bool HasValue => HeroData != null || ItemData != null;
            public bool IsHero => HeroData != null;

            public static Selection Hero(CharacterData data) => new() { HeroData = data };
            public static Selection Item(ShopItemData data) => new() { ItemData = data };

            public void Clear()
            {
                HeroData = null;
                ItemData = null;
            }
        }

        private struct CardEntry
        {
            public CharacterData HeroData;
            public ShopItemData ItemData;

            public bool IsHero => HeroData != null;

            public static CardEntry ForHero(CharacterData data) => new() { HeroData = data };
            public static CardEntry ForItem(ShopItemData data) => new() { ItemData = data };
        }
    }
}
