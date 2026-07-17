using System;
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
        private readonly IUnitPreviewService _previewService;
        private readonly ShopCatalog _catalog;

        private ShopItemData _removeAdsItem;
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

            _view.RemoveAdsClicked += OnRemoveAdsClicked;
            _view.ViewEnabled += OnViewEnabled;

            _progress.OnGoldChanged += ScheduleRebuild;
            _progress.OnOwnedChanged += ScheduleRebuild;

            FindRemoveAdsItem();
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
            _view.RemoveAdsClicked -= OnRemoveAdsClicked;
            _view.ViewEnabled -= OnViewEnabled;

            _progress.OnGoldChanged -= ScheduleRebuild;
            _progress.OnOwnedChanged -= ScheduleRebuild;
        }

        private void ScheduleRebuild() => _dirty = true;

        private void OnViewEnabled() => Rebuild();

        private void FindRemoveAdsItem()
        {
            if (_catalog.ShopItems == null)
            {
                Debug.LogWarning("[ShopController] ShopItems is null");
                return;
            }

            foreach (var item in _catalog.ShopItems)
            {
                if (item.RewardType == ShopItemRewardType.NoAds)
                {
                    _removeAdsItem = item;
                    Debug.Log($"[ShopController] Found RemoveAds item: {item.Name}");
                    break;
                }
            }

            if (_removeAdsItem == null)
                Debug.LogWarning("[ShopController] No RemoveAds item found in catalog");
        }

        private void OnRemoveAdsClicked()
        {
            Debug.Log("[ShopController] RemoveAds clicked");

            if (_removeAdsItem == null)
            {
                Debug.LogError("[ShopController] RemoveAds item is null!");
                return;
            }

            Debug.Log($"[ShopController] Purchasing: {_removeAdsItem.Name}");
            _purchaseService.Purchase(_removeAdsItem,
                onSuccess: () =>
                {
                    Debug.Log("[ShopController] RemoveAds purchase success");
                    _progress.GrantItemReward(_removeAdsItem);
                    _view.ShowPurchaseSuccessItem(_removeAdsItem.Name, _removeAdsItem.Icon);
                },
                onFailure: () => Debug.LogWarning("[ShopController] RemoveAds purchase failed"));
        }

        private void Rebuild()
        {
            _view.ClearCards();
            SpawnUnitCards();
            SpawnItemCards();
            RefreshRemoveAdsButton();
        }

        private void SpawnUnitCards()
        {
            if (_catalog.UniqueHeroes == null)
                return;

            foreach (var hero in _catalog.UniqueHeroes)
            {
                var portrait = _previewService.GetPortrait(hero, 0);
                var price = _purchaseService.GetPrice(hero.YandexProductId, hero.PriceLabel);
                _view.AddUnitCard(hero.Name, portrait, price, true, () => BuyUnit(hero));
            }
        }

        private void SpawnItemCards()
        {
            if (_catalog.ShopItems == null)
                return;

            foreach (var item in _catalog.ShopItems)
            {
                if (item.RewardType == ShopItemRewardType.NoAds)
                    continue;

                var price = _purchaseService.GetPrice(item.YandexProductId, item.PriceLabel);
                _view.AddItemCard(item.Name, item.Icon, price, () => BuyItem(item));
            }
        }

        private void BuyUnit(CharacterData hero)
        {
            Debug.Log($"[ShopController] Buying unit: {hero.Name}");
            _purchaseService.Purchase(hero,
                onSuccess: () =>
                {
                    Debug.Log($"[ShopController] Unit purchase success: {hero.Name}");
                    _progress.GrantUnit(hero);
                    var portrait = _previewService.GetPortrait(hero, 0);
                    _view.ShowPurchaseSuccessUnit(hero.Name, portrait);
                },
                onFailure: () => Debug.LogWarning($"[ShopController] Unit purchase failed: {hero.Name}"));
        }

        private void BuyItem(ShopItemData item)
        {
            _purchaseService.Purchase(item,
                onSuccess: () =>
                {
                    _progress.GrantItemReward(item);
                    _view.ShowPurchaseSuccessItem(item.Name, item.Icon);
                },
                onFailure: null);
        }

        private void RefreshRemoveAdsButton()
        {
            bool hasRemoveAds = _removeAdsItem != null;
            bool alreadyPurchased = _progress.NoAds;

            Debug.Log($"[ShopController] RefreshRemoveAds: hasItem={hasRemoveAds}, alreadyPurchased={alreadyPurchased}");

            _view.SetRemoveAdsVisible(hasRemoveAds && !alreadyPurchased);
            _view.SetRemoveAdsInteractable(hasRemoveAds && !alreadyPurchased);
        }
    }
}
