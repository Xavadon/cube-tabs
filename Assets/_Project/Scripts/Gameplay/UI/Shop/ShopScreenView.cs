using System;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Kit;
using _Project.Scripts.Gameplay.UI.Menu;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopScreenView : MonoBehaviour, IShopScreenView, IMenuScreen
    {
        private const string RootName = "shop";
        private const string GoldLabelName = "gold-label";
        private const string UnitsName = "units";
        private const string ItemsName = "items";
        private const string RemoveAdsSectionName = "remove-ads-section";
        private const string RemoveAdsButtonName = "remove-ads-button";
        private const string RemoveAdsNameName = "remove-ads-name";
        private const string RemoveAdsIconName = "remove-ads-icon";
        private const string RemoveAdsPriceName = "remove-ads-price";
        private const string RemoveAdsYanName = "remove-ads-yan";
        private const string PurchasePopupName = "purchase-popup";
        private const string CloseButtonName = "close-button";

        private const string CardClass = "card";
        private const string ShopCardClass = "shop-card";
        private const string CardNameClass = "shop-card__name";
        private const string PortraitClass = "portrait";
        private const string ButtonClass = "btn";
        private const string GreenButtonClass = "btn--green";
        private const string BuyButtonClass = "shop-buy";
        private const string CoinClass = "coin";
        private const string YanCoinClass = "coin--yan";

        [SerializeField]
        private UIDocument _document;

        private ShopScreenController _controller;
        private IPlayerProgressService _progress;
        private IAudioService _audioService;
        private ILocalizationService _localization;
        private PurchaseSuccessPopup _purchaseSuccessPopup;

        private VisualElement _root;
        private Label _goldLabel;
        private VisualElement _units;
        private VisualElement _items;
        private VisualElement _removeAdsSection;
        private Button _removeAdsButton;
        private Label _removeAdsName;
        private VisualElement _removeAdsIcon;
        private Label _removeAdsPrice;
        private VisualElement _removeAdsYan;
        private Button _closeButton;

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
            _localization = localization;
            _localization.OnLanguageChanged += UpdateLocalizedTexts;

            _root = _document.rootVisualElement.Q(RootName);
            UILocalization.Apply(_root, localization);

            _goldLabel = _root.Q<Label>(GoldLabelName);
            _units = _root.Q(UnitsName);
            _items = _root.Q(ItemsName);
            _removeAdsSection = _root.Q(RemoveAdsSectionName);
            _removeAdsButton = _root.Q<Button>(RemoveAdsButtonName);
            _removeAdsName = _root.Q<Label>(RemoveAdsNameName);
            _removeAdsIcon = _root.Q(RemoveAdsIconName);
            _removeAdsPrice = _root.Q<Label>(RemoveAdsPriceName);
            _removeAdsYan = _root.Q(RemoveAdsYanName);
            _closeButton = _root.Q<Button>(CloseButtonName);

            _purchaseSuccessPopup = new PurchaseSuccessPopup(_root.Q(PurchasePopupName), audioService, localization);

            _progress.OnGoldChanged += RefreshGold;
            RefreshGold();

            _removeAdsButton.clicked += OnRemoveAdsClicked;
            _closeButton.clicked += OnCloseButtonClicked;

            _controller = new ShopScreenController(this, progress, purchaseService, previewService, catalog, localization);

            Hide();
        }

        public void Show()
        {
            _root.SetDisplayed(true);
            ViewEnabled?.Invoke();
        }

        public void Hide()
        {
            _root.SetDisplayed(false);
        }

        private void RefreshGold()
        {
            _goldLabel.text = _progress.Gold.ToString();
        }

        private void UpdateLocalizedTexts()
        {
            UILocalization.Apply(_root, _localization);
        }

        private void LateUpdate()
        {
            _controller?.OnLateUpdate();
        }

        private void OnDestroy()
        {
            if (_localization != null)
            {
                _localization.OnLanguageChanged -= UpdateLocalizedTexts;
            }

            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
            }

            if (_root != null)
            {
                _removeAdsButton.clicked -= OnRemoveAdsClicked;
                _closeButton.clicked -= OnCloseButtonClicked;
                _purchaseSuccessPopup.Dispose();
            }

            _controller?.Dispose();
        }

        private void OnRemoveAdsClicked()
        {
            _audioService.PlayUIClick();
            RemoveAdsClicked?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            _audioService.PlayUIClick();
            Hide();
        }

        private void OnBuyClicked(ClickEvent evt)
        {
            var onBuy = (Action)((VisualElement)evt.currentTarget).userData;
            _audioService.PlayUIClick();
            onBuy?.Invoke();
        }

        public void ClearCards()
        {
            _units.Clear();
            _items.Clear();
        }

        public void AddUnitCard(string name, RenderTexture portrait, string price, bool canAfford, Action onBuy)
        {
            var card = CreateCard(_units, name, price, onBuy);
            card.image.SetImage(portrait);
            card.button.SetEnabled(canAfford);
        }

        public void AddItemCard(string name, Sprite icon, string price, Action onBuy)
        {
            var card = CreateCard(_items, name, price, onBuy);
            card.image.SetImage(icon);
        }

        public void SetRemoveAdsVisible(bool visible)
        {
            _removeAdsSection.SetDisplayed(visible);
        }

        public void SetRemoveAdsInteractable(bool interactable)
        {
            _removeAdsButton.SetEnabled(interactable);
        }

        public void SetRemoveAdsCard(string name, Sprite icon, string price)
        {
            _removeAdsName.text = name;
            _removeAdsIcon.SetImage(icon);
            PriceText.Apply(_removeAdsPrice, _removeAdsYan, price);
        }

        public void ShowPurchaseSuccessItem(string itemName, Sprite icon)
        {
            _purchaseSuccessPopup.ShowItem(itemName, icon);
        }

        public void ShowPurchaseSuccessUnit(string unitName, RenderTexture portrait)
        {
            _purchaseSuccessPopup.ShowUnit(unitName, portrait);
        }

        private (VisualElement image, Button button) CreateCard(VisualElement container, string name, string price, Action onBuy)
        {
            var card = container.AddChild(new VisualElement(), CardClass);
            card.AddToClassList(ShopCardClass);

            var image = card.AddChild(new VisualElement(), PortraitClass);
            card.AddChild(new Label(name), CardNameClass);

            var button = card.AddChild(new Button(), ButtonClass);
            button.AddToClassList(GreenButtonClass);
            button.AddToClassList(BuyButtonClass);

            var priceLabel = new Label();
            priceLabel.pickingMode = PickingMode.Ignore;
            button.Add(priceLabel);

            var yanIcon = button.AddChild(new VisualElement(), CoinClass);
            yanIcon.AddToClassList(YanCoinClass);
            yanIcon.pickingMode = PickingMode.Ignore;

            PriceText.Apply(priceLabel, yanIcon, price);

            button.userData = onBuy;
            button.RegisterCallback<ClickEvent>(OnBuyClicked);

            return (image, button);
        }
    }
}
