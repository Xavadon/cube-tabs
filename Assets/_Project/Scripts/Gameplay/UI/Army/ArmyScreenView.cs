using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Kit;
using _Project.Scripts.Gameplay.UI.Menu;
using _Project.Scripts.Gameplay.UI.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Army
{
    [RequireComponent(typeof(UIDocument))]
    public class ArmyScreenView : MonoBehaviour, IArmyScreenView, IMenuScreen
    {
        private const string RootName = "army";
        private const string GoldLabelName = "gold-label";
        private const string SlotCountLabelName = "slot-count-label";
        private const string ArmyListName = "army-list";
        private const string ReserveListName = "reserve-list";
        private const string ArmySectionName = "army-section";
        private const string ReserveSectionName = "reserve-section";
        private const string ArmyPrevName = "army-prev";
        private const string ArmyNextName = "army-next";
        private const string ReservePrevName = "reserve-prev";
        private const string ReserveNextName = "reserve-next";
        private const string ToArmyButtonName = "to-army-button";
        private const string ToReserveButtonName = "to-reserve-button";
        private const string PreviewName = "preview";
        private const string SelectedInfoName = "selected-info";
        private const string SelectedNameName = "selected-name";
        private const string SelectedHpName = "selected-hp";
        private const string SelectedDamageName = "selected-damage";
        private const string SelectedSpeedName = "selected-speed";
        private const string SellButtonName = "sell-button";
        private const string SellPriceName = "sell-price";
        private const string EvolutionName = "evolution";
        private const string EvolutionOptionsName = "evolution-options";
        private const string BuyButtonName = "buy-button";
        private const string BuyCostName = "buy-cost";
        private const string SlotUpgradeButtonName = "slot-upgrade-button";
        private const string SlotUpgradeCostName = "slot-upgrade-cost";
        private const string GoldRewardButtonName = "gold-reward-button";
        private const string GoldRewardAmountName = "gold-reward-amount";
        private const string CloseButtonName = "close-button";

        private const string CardClass = "unit-card";
        private const string CardSelectedClass = "unit-card--selected";
        private const string CardNameClass = "unit-card__name";
        private const string CardCountClass = "unit-card__count";
        private const string PortraitClass = "portrait";
        private const string HealthFormat = "0";
        private const string DamageFormat = "0";
        private const string SpeedFormat = "0.#";

        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private float _dragRotationSpeed = 0.5f;

        private readonly List<VisualElement> _cards = new();

        private ArmyScreenController _controller;
        private IPlayerProgressService _progress;
        private IAudioService _audioService;
        private ILocalizationService _localization;
        private EvolutionPanel _evolutionPanel;
        private CardDragHandler _cardDrag;
        private CardStrip _armyStrip;
        private CardStrip _reserveStrip;

        private VisualElement _root;
        private Label _goldLabel;
        private Label _slotCountLabel;
        private ScrollView _armyList;
        private ScrollView _reserveList;
        private Button _toArmyButton;
        private Button _toReserveButton;
        private VisualElement _preview;
        private VisualElement _selectedInfo;
        private Label _selectedNameLabel;
        private Label _selectedHpLabel;
        private Label _selectedDamageLabel;
        private Label _selectedSpeedLabel;
        private Button _sellButton;
        private Label _sellPriceLabel;
        private Button _buyButton;
        private Button _slotUpgradeButton;
        private Label _slotUpgradeCostLabel;
        private Button _goldRewardButton;
        private Button _closeButton;

        private PreviewHandle _currentPreview;
        private Transform _currentPreviewModel;
        private Quaternion _defaultFullBodyRotation;

        public event Action BuyClicked;
        public event Action SlotUpgradeClicked;
        public event Action ToArmyClicked;
        public event Action ToReserveClicked;
        public event Action SellClicked;
        public event Action<int> CardClicked;
        public event Action<int, bool> CardDropped;
        public event Action ViewEnabled;
        public event Action GoldRewardClicked;

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog,
            IUnitPreviewService previewService, IAdService adService, IAudioService audioService,
            ILocalizationService localization)
        {
            _progress = progress;
            _audioService = audioService;
            _localization = localization;
            _defaultFullBodyRotation = previewService.DefaultFullBodyRotation;

            QueryElements();

            _evolutionPanel = new EvolutionPanel(_root.Q(EvolutionName), _root.Q(EvolutionOptionsName),
                progress, catalog.EvolutionCatalog, previewService, audioService, localization);

            _cardDrag = new CardDragHandler(_root, _root.Q(ArmySectionName), _root.Q(ReserveSectionName));
            _cardDrag.Clicked += OnCardClicked;
            _cardDrag.Dropped += OnCardDropped;

            _armyStrip = new CardStrip(_armyList, _root.Q<Button>(ArmyPrevName), _root.Q<Button>(ArmyNextName), audioService);
            _reserveStrip = new CardStrip(_reserveList, _root.Q<Button>(ReservePrevName), _root.Q<Button>(ReserveNextName), audioService);

            if (catalog.BaseUnit != null)
            {
                _root.Q<Label>(BuyCostName).text = catalog.BaseUnit.PriceAsHero.ToString();
            }

            _root.Q<Label>(GoldRewardAmountName).text = $"+{ArmyScreenController.GoldRewardAmount}";

            _progress.OnGoldChanged += RefreshGold;
            _progress.OnGoldChanged += RefreshSlotUpgrade;
            _progress.OnArmyChanged += RefreshSlotCount;
            _progress.OnArmyChanged += RefreshSlotUpgrade;
            _localization.OnLanguageChanged += UpdateLocalizedTexts;

            RefreshGold();
            RefreshSlotUpgrade();
            UpdateLocalizedTexts();

            _buyButton.clicked += OnBuyButtonClicked;
            _slotUpgradeButton.clicked += OnSlotUpgradeButtonClicked;
            _toArmyButton.clicked += OnToArmyButtonClicked;
            _toReserveButton.clicked += OnToReserveButtonClicked;
            _sellButton.clicked += OnSellButtonClicked;
            _goldRewardButton.clicked += OnGoldRewardButtonClicked;
            _closeButton.clicked += OnCloseButtonClicked;

            _preview.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            _preview.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            _preview.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);

            _goldRewardButton.SetDisplayed(false);

            _controller = new ArmyScreenController(this, progress, previewService, catalog, adService, localization);
        }

        public void Show()
        {
            SetActive(true);
        }

        public void Hide()
        {
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            _root.SetDisplayed(active);
            _currentPreview.SetActive(active);
            _evolutionPanel.SetPreviewsActive(active);

            if (active)
            {
                ViewEnabled?.Invoke();
            }
        }

        private void QueryElements()
        {
            _root = _document.rootVisualElement.Q(RootName);
            _goldLabel = _root.Q<Label>(GoldLabelName);
            _slotCountLabel = _root.Q<Label>(SlotCountLabelName);
            _armyList = _root.Q<ScrollView>(ArmyListName);
            _reserveList = _root.Q<ScrollView>(ReserveListName);
            _toArmyButton = _root.Q<Button>(ToArmyButtonName);
            _toReserveButton = _root.Q<Button>(ToReserveButtonName);
            _preview = _root.Q(PreviewName);
            _selectedInfo = _root.Q(SelectedInfoName);
            _selectedNameLabel = _root.Q<Label>(SelectedNameName);
            _selectedHpLabel = _root.Q<Label>(SelectedHpName);
            _selectedDamageLabel = _root.Q<Label>(SelectedDamageName);
            _selectedSpeedLabel = _root.Q<Label>(SelectedSpeedName);
            _sellButton = _root.Q<Button>(SellButtonName);
            _sellPriceLabel = _root.Q<Label>(SellPriceName);
            _buyButton = _root.Q<Button>(BuyButtonName);
            _slotUpgradeButton = _root.Q<Button>(SlotUpgradeButtonName);
            _slotUpgradeCostLabel = _root.Q<Label>(SlotUpgradeCostName);
            _goldRewardButton = _root.Q<Button>(GoldRewardButtonName);
            _closeButton = _root.Q<Button>(CloseButtonName);
        }

        private void RefreshGold()
        {
            _goldLabel.text = _progress.Gold.ToString();
        }

        private void RefreshSlotCount()
        {
            _slotCountLabel.text = _localization.Get(LocalizationKeys.Army.Slots, _progress.ArmyUnits.Count, _progress.ArmySlots);
        }

        private void UpdateLocalizedTexts()
        {
            UILocalization.Apply(_root, _localization);
            RefreshSlotCount();
        }

        private void RefreshSlotUpgrade()
        {
            bool maxed = _progress.ArmySlots >= _progress.MaxArmySlots;
            _slotUpgradeButton.SetDisplayed(!maxed);

            if (!maxed)
            {
                int cost = _progress.GetSlotUpgradeCost();
                _slotUpgradeCostLabel.text = cost.ToString();
                _slotUpgradeButton.SetEnabled(_progress.CanAfford(cost));
            }
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            _preview.CapturePointer(evt.pointerId);
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (_currentPreviewModel == null || !_preview.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            _currentPreviewModel.Rotate(Vector3.up, -evt.deltaPosition.x * _dragRotationSpeed, Space.World);
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            _preview.ReleasePointer(evt.pointerId);
        }

        private void LateUpdate()
        {
            _controller?.OnLateUpdate();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
                _progress.OnGoldChanged -= RefreshSlotUpgrade;
                _progress.OnArmyChanged -= RefreshSlotCount;
                _progress.OnArmyChanged -= RefreshSlotUpgrade;
            }

            if (_localization != null)
            {
                _localization.OnLanguageChanged -= UpdateLocalizedTexts;
            }

            if (_root != null)
            {
                _buyButton.clicked -= OnBuyButtonClicked;
                _slotUpgradeButton.clicked -= OnSlotUpgradeButtonClicked;
                _toArmyButton.clicked -= OnToArmyButtonClicked;
                _toReserveButton.clicked -= OnToReserveButtonClicked;
                _sellButton.clicked -= OnSellButtonClicked;
                _goldRewardButton.clicked -= OnGoldRewardButtonClicked;
                _closeButton.clicked -= OnCloseButtonClicked;
                _cardDrag.Clicked -= OnCardClicked;
                _cardDrag.Dropped -= OnCardDropped;
                _armyStrip.Dispose();
                _reserveStrip.Dispose();
            }

            _controller?.Dispose();
        }

        private void OnBuyButtonClicked()
        {
            _audioService.PlayUIClick();
            BuyClicked?.Invoke();
        }

        private void OnSlotUpgradeButtonClicked()
        {
            _audioService.PlayUIClick();
            SlotUpgradeClicked?.Invoke();
        }

        private void OnToArmyButtonClicked()
        {
            _audioService.PlayUIClick();
            ToArmyClicked?.Invoke();
        }

        private void OnToReserveButtonClicked()
        {
            _audioService.PlayUIClick();
            ToReserveClicked?.Invoke();
        }

        private void OnSellButtonClicked()
        {
            _audioService.PlayUIClick();
            SellClicked?.Invoke();
        }

        private void OnGoldRewardButtonClicked()
        {
            _audioService.PlayUIClick();
            GoldRewardClicked?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            _audioService.PlayUIClick();
            Hide();
        }

        private void OnCardClicked(VisualElement card)
        {
            int index = _cards.IndexOf(card);
            _audioService.PlayUIClick();
            CardClicked?.Invoke(index);
        }

        private void OnCardDropped(VisualElement card, bool toArmy)
        {
            int index = _cards.IndexOf(card);
            _audioService.PlayUIClick();
            CardDropped?.Invoke(index, toArmy);
        }

        public void ClearCards()
        {
            _cardDrag.Cancel();
            _armyList.Clear();
            _reserveList.Clear();
            _cards.Clear();
        }

        public void AddCard(string name, int count, RenderTexture portrait, bool isInArmy)
        {
            var card = new VisualElement();
            card.AddToClassList(CardClass);

            var portraitElement = card.AddChild(new VisualElement(), PortraitClass);
            portraitElement.pickingMode = PickingMode.Ignore;
            portraitElement.SetImage(portrait);

            var nameLabel = card.AddChild(new Label(name), CardNameClass);
            nameLabel.pickingMode = PickingMode.Ignore;

            if (count > 1)
            {
                var countLabel = card.AddChild(new Label($"x{count}"), CardCountClass);
                countLabel.pickingMode = PickingMode.Ignore;
            }

            _cardDrag.Register(card);

            if (isInArmy)
            {
                _armyList.Add(card);
            }
            else
            {
                _reserveList.Add(card);
            }

            _cards.Add(card);
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < _cards.Count)
            {
                _cards[index].EnableInClassList(CardSelectedClass, selected);
            }
        }

        public void SetToArmyInteractable(bool interactable)
        {
            _toArmyButton.SetEnabled(interactable);
        }

        public void SetToReserveInteractable(bool interactable)
        {
            _toReserveButton.SetEnabled(interactable);
        }

        public void ShowSell(int price)
        {
            _sellPriceLabel.text = $"+{price}";
            _sellButton.SetVisible(true);
        }

        public void HideSell()
        {
            _sellButton.SetVisible(false);
        }

        public void ShowSelectedStats(string name, float hp, float damage, float speed)
        {
            _selectedInfo.SetVisible(true);
            _selectedNameLabel.text = name;
            _selectedHpLabel.text = hp.ToString(HealthFormat);
            _selectedDamageLabel.text = damage.ToString(DamageFormat);
            _selectedSpeedLabel.text = speed.ToString(SpeedFormat);
        }

        public void HideSelectedStats()
        {
            _selectedInfo.SetVisible(false);
        }

        public void ShowFullBodyPreview(PreviewHandle handle)
        {
            _currentPreview.SetActive(false);
            _currentPreview = handle;
            _currentPreview.SetActive(true);

            _preview.SetImage(handle.Texture);
            _preview.SetVisible(true);

            _currentPreviewModel = handle.Model;
            _currentPreviewModel.rotation = _defaultFullBodyRotation;
        }

        public void HideFullBodyPreview()
        {
            _preview.SetVisible(false);
            _currentPreview.SetActive(false);
            _currentPreview = default;
            _currentPreviewModel = null;
        }

        public void ShowEvolution(int instanceId, CharacterData data, int tierIndex)
        {
            _evolutionPanel.Show(instanceId, data, tierIndex);
        }

        public void HideEvolution()
        {
            _evolutionPanel.Hide();
        }

        public void SetSlotUpgradeInteractable(bool interactable)
        {
            _slotUpgradeButton.SetEnabled(interactable);
        }

        public void SetSlotUpgradeCost(string text)
        {
            _slotUpgradeCostLabel.text = text;
        }

        public void SetGoldRewardButtonVisible(bool visible)
        {
            _goldRewardButton.SetDisplayed(visible);
        }
    }
}
