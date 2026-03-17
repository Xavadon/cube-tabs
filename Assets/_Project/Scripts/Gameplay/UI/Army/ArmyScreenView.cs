using System;
using System.Collections.Generic;
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
        private TextMeshProUGUI _buyButtonCostLabel;

        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [Header("Transfer")]
        [SerializeField]
        private Button _transferButton;

        [SerializeField]
        private TextMeshProUGUI _transferButtonLabel;

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

        private ArmyScreenController _controller;
        private IPlayerProgressService _progress;
        private ShopCatalog _catalog;
        private readonly List<ArmyUnitCardUI> _cards = new();

        private Transform _currentPreviewModel;
        private Quaternion _defaultFullBodyRotation;

        public event Action BuyClicked;
        public event Action TransferClicked;
        public event Action<int> CardClicked;
        public event Action ViewEnabled;

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog,
            IUnitPreviewService previewService)
        {
            _progress = progress;
            _catalog = catalog;
            _defaultFullBodyRotation = previewService.DefaultFullBodyRotation;

            // View binds to Model directly for simple data display (Supervising Controller)
            _progress.OnGoldChanged += RefreshGold;
            _progress.OnArmyChanged += RefreshSlotCount;

            if (_evolutionPanel != null)
                _evolutionPanel.Initialize(progress, catalog.EvolutionCatalog, previewService);

            RefreshGold();
            RefreshSlotCount();

            if (_catalog.BaseUnit != null)
                _buyButtonCostLabel.text = _catalog.BaseUnit.Price.ToString();

            SetupPreviewDrag();

            _controller = new ArmyScreenController(this, progress, previewService, catalog);

            _buyButton.onClick.AddListener(OnBuyButtonClicked);
            _transferButton.onClick.AddListener(OnTransferButtonClicked);
        }

        // --- Simple data binding (View → Model) ---

        private void RefreshGold()
        {
            if (_goldLabel != null)
                _goldLabel.text = _progress.Gold.ToString();

            _buyButton.interactable = _catalog.BaseUnit != null
                                      && _progress.CanAfford(_catalog.BaseUnit.Price);
        }

        private void RefreshSlotCount()
        {
            _slotCountLabel.text = $"{_progress.ArmyUnits.Count}/{_progress.ArmySlots}";
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
            _transferButton.onClick.RemoveListener(OnTransferButtonClicked);

            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
                _progress.OnArmyChanged -= RefreshSlotCount;
            }

            _controller?.Dispose();
        }

        private void OnBuyButtonClicked() => BuyClicked?.Invoke();
        private void OnTransferButtonClicked() => TransferClicked?.Invoke();

        // --- IArmyScreenView (commanded by Controller) ---

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void ClearCards()
        {
            ClearContainer(_armyContainer);
            ClearContainer(_reserveContainer);
            _cards.Clear();
        }

        public void AddCard(string name, int count, RenderTexture portrait, float hp, float damage, float speed, bool isInArmy)
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
            card.Init(name, count, portrait, hp, damage, speed, () => CardClicked?.Invoke(index));
            _cards.Add(card);
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < _cards.Count)
            {
                _cards[index].SetSelected(selected);
            }
        }

        public void SetTransferVisible(bool visible) => _transferButton.gameObject.SetActive(visible);
        public void SetTransferLabel(string text) => _transferButtonLabel.text = text;
        public void SetTransferInteractable(bool interactable) => _transferButton.interactable = interactable;

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

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
