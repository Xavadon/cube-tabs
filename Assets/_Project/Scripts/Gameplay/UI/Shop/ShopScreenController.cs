using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopScreenController : IDisposable
    {
        private readonly IShopScreenView _view;
        private readonly IPlayerProgressService _progress;
        private readonly IPurchaseService _purchaseService;
        private readonly IUnitPreviewService _previewService;
        private readonly ShopCatalog _catalog;
        private readonly List<CardEntry> _cardEntries = new();

        private Selection _selection;
        private bool _dirty;

        public ShopScreenController(
            IShopScreenView view,
            IPlayerProgressService progress,
            IPurchaseService purchaseService,
            IUnitPreviewService previewService,
            ShopCatalog catalog)
        {
            _view = view;
            _progress = progress;
            _purchaseService = purchaseService;
            _previewService = previewService;
            _catalog = catalog;

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
                var portrait = _previewService.GetPortrait(hero, 0);
                var tier = hero.GetTier(0);
                float hp = tier.Stats.Health;
                float damage = tier.Stats.Damage;
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
                var handle = _previewService.GetFullBody(_selection.HeroData, 0);
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
                    _view.SetBuyLabel($"Купить ({_selection.HeroData.PriceAsHero})");
                    _view.SetBuyInteractable(_progress.CanAfford(_selection.HeroData.PriceAsHero));
                }
            }
            else
            {
                _view.SetBuyLabel($"Купить ({_selection.ItemData.PriceLabel})");
                _view.SetBuyInteractable(true);
            }
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
