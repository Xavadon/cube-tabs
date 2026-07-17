using System;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopScreenView : MonoBehaviour, IShopScreenView
    {
        [Header("Card Prefabs")]
        [SerializeField]
        private ShopUnitCardUI _unitCardPrefab;

        [SerializeField]
        private ShopCardUI _itemCardPrefab;

        [Header("Containers")]
        [SerializeField]
        private Transform _unitsContainer;

        [SerializeField]
        private Transform _itemsContainer;

        [Header("Gold")]
        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [Header("Remove Ads")]
        [SerializeField]
        private Button _removeAdsButton;

        [Header("Close")]
        [SerializeField]
        private Button _closeButton;

        [Header("Purchase Success Popup")]
        [SerializeField]
        private PurchaseSuccessPopup _purchaseSuccessPopup;

        private ShopScreenController _controller;
        private IPlayerProgressService _progress;
        private IAudioService _audioService;

        public event Action RemoveAdsClicked;
        public event Action ViewEnabled;

        public void Initialize(
            IPlayerProgressService progress,
            IPurchaseService purchaseService,
            IUnitPreviewService previewService,
            ShopCatalog catalog,
            IAudioService audioService,
            ILocalizationService localization)
        {
            _progress = progress;
            _audioService = audioService;

            _progress.OnGoldChanged += RefreshGold;
            RefreshGold();

            if (_purchaseSuccessPopup != null)
                _purchaseSuccessPopup.Initialize(audioService, localization);

            _controller = new ShopScreenController(
                this, progress, purchaseService, previewService, catalog);

            if (_removeAdsButton != null)
                _removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseButtonClicked);

            gameObject.SetActive(false);
        }

        private void RefreshGold()
        {
            if (_goldLabel != null)
                _goldLabel.text = _progress.Gold.ToString();
        }

        private void OnEnable() => ViewEnabled?.Invoke();

        private void LateUpdate() => _controller?.OnLateUpdate();

        private void OnDestroy()
        {
            if (_removeAdsButton != null)
                _removeAdsButton.onClick.RemoveListener(OnRemoveAdsClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);

            if (_progress != null)
                _progress.OnGoldChanged -= RefreshGold;

            _controller?.Dispose();
        }

        private void OnRemoveAdsClicked()
        {
            Debug.Log("[ShopView] RemoveAds button clicked");
            _audioService?.PlayUIClick();
            RemoveAdsClicked?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            _audioService?.PlayUIClick();
            gameObject.SetActive(false);
        }

        public void ClearCards()
        {
            ClearContainer(_unitsContainer);
            ClearContainer(_itemsContainer);
        }

        private void ClearContainer(Transform container)
        {
            if (container == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        public void AddUnitCard(string name, RenderTexture portrait, string price, bool canAfford, Action onBuy)
        {
            var card = Instantiate(_unitCardPrefab, _unitsContainer);
            card.Init(name, portrait, price, canAfford, () =>
            {
                _audioService?.PlayUIClick();
                onBuy?.Invoke();
            });
        }

        public void AddItemCard(string name, Sprite icon, string price, Action onBuy)
        {
            var card = Instantiate(_itemCardPrefab, _itemsContainer);
            card.Init(name, icon, price, () =>
            {
                _audioService?.PlayUIClick();
                onBuy?.Invoke();
            });
        }

        public void SetRemoveAdsVisible(bool visible)
        {
            if (_removeAdsButton != null)
                _removeAdsButton.gameObject.SetActive(visible);
        }

        public void SetRemoveAdsInteractable(bool interactable)
        {
            if (_removeAdsButton != null)
                _removeAdsButton.interactable = interactable;
        }

        public void ShowPurchaseSuccessItem(string itemName, Sprite icon)
        {
            if (_purchaseSuccessPopup != null)
                _purchaseSuccessPopup.ShowItem(itemName, icon);
        }

        public void ShowPurchaseSuccessUnit(string unitName, RenderTexture portrait)
        {
            if (_purchaseSuccessPopup != null)
                _purchaseSuccessPopup.ShowUnit(unitName, portrait);
        }
    }
}
