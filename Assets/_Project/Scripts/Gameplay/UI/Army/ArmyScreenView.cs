using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyScreenView : MonoBehaviour, IArmyScreenView
    {
        [SerializeField]
        private ArmyUnitCardUI _cardPrefab;

        [Header("Army")]
        [SerializeField]
        private Transform _armyContainer;

        [FormerlySerializedAs("_backlogContainer")]
        [Header("Reserve")]
        [SerializeField]
        private Transform _reserveContainer;

        [SerializeField]
        private TextMeshProUGUI _slotCountLabel;

        [Header("Buy")]
        [SerializeField]
        private Button _buyButton;

        [SerializeField]
        private TextMeshProUGUI _buyButtonLabel;

        [SerializeField]
        private TextMeshProUGUI _buyButtonCostLabel;

        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [SerializeField]
        private Button _goldRewardButton;

        [Header("Slot Upgrade")]
        [SerializeField]
        private Button _slotUpgradeButton;

        [SerializeField]
        private TextMeshProUGUI _slotUpgradeLabel;

        [SerializeField]
        private TextMeshProUGUI _slotUpgradeCostLabel;

        [Header("Transfer")]
        [SerializeField]
        private Button _toArmyButton;

        [SerializeField]
        private Button _toReserveButton;

        [Header("Selected Stats")]
        [SerializeField]
        private GameObject _selectedStatsPanel;

        [SerializeField]
        private TextMeshProUGUI _selectedNameLabel;

        [SerializeField]
        private TextMeshProUGUI _selectedHpLabel;

        [SerializeField]
        private TextMeshProUGUI _selectedDamageLabel;

        [SerializeField]
        private TextMeshProUGUI _selectedSpeedLabel;

        [Header("Full Body Preview")]
        [SerializeField]
        private RawImage _fullBodyPreviewImage;

        [SerializeField]
        private Image _fullBodyPreviewFrame;

        [SerializeField]
        private float _dragRotationSpeed = 0.5f;

        [Header("Evolution")]
        [SerializeField]
        private EvolutionPanelUI _evolutionPanel;

        [Header("Close")]
        [SerializeField]
        private Button _closeButton;

        private ArmyScreenController _controller;
        private IPlayerProgressService _progress;
        private ShopCatalog _catalog;
        private IAudioService _audioService;
        private ILocalizationService _localization;
        private readonly List<ArmyUnitCardUI> _cards = new();

        private Transform _currentPreviewModel;
        private Quaternion _defaultFullBodyRotation;

        public event Action BuyClicked;
        public event Action SlotUpgradeClicked;
        public event Action ToArmyClicked;
        public event Action ToReserveClicked;
        public event Action<int> CardClicked;
        public event Action ViewEnabled;
        public event Action GoldRewardClicked;

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog,
            IUnitPreviewService previewService, IAdService adService, IAudioService audioService,
            ILocalizationService localization)
        {
            _progress = progress;
            _catalog = catalog;
            _audioService = audioService;
            _localization = localization;
            _defaultFullBodyRotation = previewService.DefaultFullBodyRotation;

            // View binds to Model directly for simple data display (Supervising Controller)
            _progress.OnGoldChanged += RefreshGold;
            _progress.OnGoldChanged += RefreshSlotUpgrade;
            _progress.OnArmyChanged += RefreshSlotCount;
            _progress.OnArmyChanged += RefreshSlotUpgrade;
            _localization.OnLanguageChanged += UpdateLocalizedTexts;

            if (_evolutionPanel != null)
            {
                _evolutionPanel.Initialize(progress, catalog.EvolutionCatalog, previewService, audioService, localization);
            }

            RefreshGold();
            RefreshSlotCount();
            RefreshSlotUpgrade();
            UpdateLocalizedTexts();

            if (_catalog.BaseUnit != null)
            {
                _buyButtonCostLabel.text = _catalog.BaseUnit.PriceAsHero.ToString();
            }

            SetupPreviewDrag();

            _controller = new ArmyScreenController(this, progress, previewService, catalog, adService);

            _buyButton.onClick.AddListener(OnBuyButtonClicked);
            _slotUpgradeButton.onClick.AddListener(OnSlotUpgradeButtonClicked);
            _toArmyButton.onClick.AddListener(OnToArmyButtonClicked);
            _toReserveButton.onClick.AddListener(OnToReserveButtonClicked);

            if (_goldRewardButton != null)
            {
                _goldRewardButton.onClick.AddListener(OnGoldRewardButtonClicked);
                _goldRewardButton.gameObject.SetActive(false);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        // --- Simple data binding (View → Model) ---

        private void RefreshGold()
        {
            if (_goldLabel != null)
                _goldLabel.text = _progress.Gold.ToString();
        }

        private void RefreshSlotCount()
        {
            _slotCountLabel.text = _localization.Get(LocalizationKeys.Army.Slots, _progress.ArmyUnits.Count, _progress.ArmySlots);
        }

        private void UpdateLocalizedTexts()
        {
            RefreshSlotCount();

            if (_buyButtonLabel != null)
            {
                _buyButtonLabel.text = _localization.Get(LocalizationKeys.Army.Buy);
            }

            if (_slotUpgradeLabel != null)
            {
                _slotUpgradeLabel.text = _localization.Get(LocalizationKeys.Army.UpgradeSlot);
            }
        }

        private void RefreshSlotUpgrade()
        {
            bool maxed = _progress.ArmySlots >= _progress.MaxArmySlots;
            int cost = _progress.GetSlotUpgradeCost();

            _slotUpgradeButton.gameObject.SetActive(!maxed);

            if (!maxed)
            {
                _slotUpgradeCostLabel.text = cost.ToString();
                _slotUpgradeButton.interactable = _progress.CanAfford(cost);
            }
        }

        // --- Preview drag rotation ---

        private void SetupPreviewDrag()
        {
            if (_fullBodyPreviewImage == null)
                return;

            var trigger = _fullBodyPreviewImage.gameObject.AddComponent<EventTrigger>();

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(OnPreviewDrag);
            trigger.triggers.Add(dragEntry);
        }

        private void OnPreviewDrag(BaseEventData data)
        {
            if (_currentPreviewModel == null)
                return;

            var pointerData = (PointerEventData)data;
            _currentPreviewModel.Rotate(Vector3.up, -pointerData.delta.x * _dragRotationSpeed, Space.World);
        }

        // --- Lifecycle ---

        private void OnEnable() => ViewEnabled?.Invoke();

        private void LateUpdate() => _controller?.OnLateUpdate();

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            _slotUpgradeButton.onClick.RemoveListener(OnSlotUpgradeButtonClicked);
            _toArmyButton.onClick.RemoveListener(OnToArmyButtonClicked);
            _toReserveButton.onClick.RemoveListener(OnToReserveButtonClicked);

            if (_goldRewardButton != null)
                _goldRewardButton.onClick.RemoveListener(OnGoldRewardButtonClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);

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

            _controller?.Dispose();
        }

        private void OnBuyButtonClicked()
        {
            _audioService?.PlayUIClick();
            BuyClicked?.Invoke();
        }

        private void OnSlotUpgradeButtonClicked()
        {
            _audioService?.PlayUIClick();
            SlotUpgradeClicked?.Invoke();
        }

        private void OnToArmyButtonClicked()
        {
            _audioService?.PlayUIClick();
            ToArmyClicked?.Invoke();
        }

        private void OnToReserveButtonClicked()
        {
            _audioService?.PlayUIClick();
            ToReserveClicked?.Invoke();
        }

        private void OnGoldRewardButtonClicked()
        {
            _audioService?.PlayUIClick();
            GoldRewardClicked?.Invoke();
        }

        private void OnCloseButtonClicked()
        {
            _audioService?.PlayUIClick();
            gameObject.SetActive(false);
        }

        // --- IArmyScreenView (commanded by Controller) ---

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void ClearCards()
        {
            ClearContainer(_armyContainer);
            ClearContainer(_reserveContainer);
            _cards.Clear();
        }

        public void AddCard(string name, int count, RenderTexture portrait, bool isInArmy)
        {
            Transform container;

            if (isInArmy)
            {
                container = _armyContainer;
            }
            else
            {
                container = _reserveContainer;
            }

            var card = Instantiate(_cardPrefab, container);
            int index = _cards.Count;
            card.Init(name, count, portrait, () =>
            {
                _audioService?.PlayUIClick();
                CardClicked?.Invoke(index);
            });
            _cards.Add(card);
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < _cards.Count)
            {
                _cards[index].SetSelected(selected);
            }
        }

        public void SetToArmyInteractable(bool interactable) => _toArmyButton.interactable = interactable;
        public void SetToReserveInteractable(bool interactable) => _toReserveButton.interactable = interactable;

        public void ShowSelectedStats(string name, float hp, float damage, float speed)
        {
            _selectedStatsPanel.SetActive(true);
            _selectedNameLabel.text = name;
            _selectedHpLabel.text = hp.ToString("0");
            _selectedDamageLabel.text = damage.ToString("0");
            _selectedSpeedLabel.text = speed.ToString("0.#");
        }

        public void HideSelectedStats()
        {
            _selectedStatsPanel.SetActive(false);
        }

        public void ShowFullBodyPreview(PreviewHandle handle)
        {
            if (_fullBodyPreviewImage == null)
                return;

            _fullBodyPreviewImage.texture = handle.Texture;
            _fullBodyPreviewImage.gameObject.SetActive(true);
            _fullBodyPreviewFrame.gameObject.SetActive(true);

            _currentPreviewModel = handle.Model;
            _currentPreviewModel.rotation = _defaultFullBodyRotation;
        }

        public void HideFullBodyPreview()
        {
            if (_fullBodyPreviewImage != null)
            {
                _fullBodyPreviewImage.gameObject.SetActive(false);
                _fullBodyPreviewFrame.gameObject.SetActive(false);
            }

            _currentPreviewModel = null;
        }

        public void ShowEvolution(int instanceId, CharacterData data, int tierIndex)
        {
            if (_evolutionPanel != null)
            {
                _evolutionPanel.Show(instanceId, data, tierIndex);
            }
        }

        public void HideEvolution()
        {
            if (_evolutionPanel != null)
            {
                _evolutionPanel.Hide();
            }
        }

        public void SetSlotUpgradeInteractable(bool interactable) =>
            _slotUpgradeButton.interactable = interactable;

        public void SetSlotUpgradeCost(string text) =>
            _slotUpgradeCostLabel.text = text;

        public void SetGoldRewardButtonVisible(bool visible)
        {
            if (_goldRewardButton != null)
                _goldRewardButton.gameObject.SetActive(visible);
        }

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
