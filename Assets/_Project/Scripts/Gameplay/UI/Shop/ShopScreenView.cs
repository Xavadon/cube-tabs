using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI;
using _Project.Scripts.Gameplay.UI.Army;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopScreenView : MonoBehaviour, IShopScreenView
    {
        [Header("Card Prefabs")]
        [SerializeField]
        private ArmyUnitCardUI _heroCardPrefab;

        [SerializeField]
        private ShopCardUI _itemCardPrefab;

        [SerializeField]
        private Transform _cardsContainer;

        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [Header("Preview")]
        [SerializeField]
        private GameObject _cardPreviewParent;

        [SerializeField]
        private RawImage _unitPreviewImage;
        
        [SerializeField]
        private Image _itemPreviewImage;

        [SerializeField]
        private float _dragRotationSpeed = 0.5f;

        [Header("Buy")]
        [SerializeField]
        private Button _buyButton;

        [SerializeField]
        private TextMeshProUGUI _buyButtonLabel;

        private ShopScreenController _controller;
        private IPlayerProgressService _progress;
        private readonly List<ISelectableCard> _cards = new();

        private Transform _currentPreviewModel;
        private Quaternion _defaultFullBodyRotation;

        public event Action<int> CardClicked;
        public event Action BuyClicked;
        public event Action ViewEnabled;

        public void Initialize(
            IPlayerProgressService progress,
            IPurchaseService purchaseService,
            IUnitPreviewService previewService,
            ShopCatalog catalog)
        {
            _progress = progress;
            _defaultFullBodyRotation = previewService.DefaultFullBodyRotation;

            _progress.OnGoldChanged += RefreshGold;
            RefreshGold();

            SetupPreviewDrag();

            _controller = new ShopScreenController(
                this, progress, purchaseService, previewService, catalog);

            _buyButton.onClick.AddListener(OnBuyButtonClicked);
            _cardPreviewParent.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        // --- Simple data binding (View → Model, Supervising Controller) ---

        private void RefreshGold()
        {
            if (_goldLabel != null)
            {
                _goldLabel.text = _progress.Gold.ToString();
            }
        }

        // --- Preview drag rotation ---

        private void SetupPreviewDrag()
        {
            if (_unitPreviewImage == null)
            {
                return;
            }

            var trigger = _unitPreviewImage.gameObject.AddComponent<EventTrigger>();

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(OnPreviewDrag);
            trigger.triggers.Add(dragEntry);
        }

        private void OnPreviewDrag(BaseEventData data)
        {
            if (_currentPreviewModel == null)
            {
                return;
            }

            var pointerData = (PointerEventData)data;
            _currentPreviewModel.Rotate(Vector3.up, -pointerData.delta.x * _dragRotationSpeed, Space.World);
        }

        // --- Lifecycle ---

        private void OnEnable() => ViewEnabled?.Invoke();

        private void LateUpdate() => _controller?.OnLateUpdate();

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);

            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
            }

            _controller?.Dispose();
        }

        private void OnBuyButtonClicked() => BuyClicked?.Invoke();

        // --- IShopScreenView (commanded by Controller) ---

        public void ClearCards()
        {
            for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsContainer.GetChild(i).gameObject);
            }

            _cards.Clear();
        }

        public void AddHeroCard(string name, RenderTexture portrait)
        {
            var card = Instantiate(_heroCardPrefab, _cardsContainer);
            int index = _cards.Count;
            card.Init(name, 1, portrait, () => CardClicked?.Invoke(index));
            _cards.Add(card);
        }

        public void AddItemCard(string name, Sprite icon)
        {
            var card = Instantiate(_itemCardPrefab, _cardsContainer);
            int index = _cards.Count;
            card.Init(name, icon, () => CardClicked?.Invoke(index));
            _cards.Add(card);
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < _cards.Count)
            {
                _cards[index].SetSelected(selected);
            }
        }

        public void ShowUnitPreview(PreviewHandle handle)
        {
            _cardPreviewParent.SetActive(true);

            if (_unitPreviewImage != null)
            {
                _unitPreviewImage.texture = handle.Texture;
                _unitPreviewImage.gameObject.SetActive(true);
            }

            if (_itemPreviewImage != null)
            {
                _itemPreviewImage.gameObject.SetActive(false);
            }

            _currentPreviewModel = handle.Model;
            _currentPreviewModel.rotation = _defaultFullBodyRotation;
        }

        public void ShowItemPreview(Sprite icon)
        {
            _cardPreviewParent.SetActive(true);

            if (_itemPreviewImage != null)
            {
                _itemPreviewImage.sprite = icon;
                _itemPreviewImage.gameObject.SetActive(true);
            }

            if (_unitPreviewImage != null)
            {
                _unitPreviewImage.gameObject.SetActive(false);
            }

            _currentPreviewModel = null;
        }

        public void HidePreview()
        {
            _cardPreviewParent.SetActive(false);
            _currentPreviewModel = null;
        }

        public void SetBuyVisible(bool visible)
        {
            _buyButton.gameObject.SetActive(visible);
        }

        public void SetBuyInteractable(bool interactable)
        {
            _buyButton.interactable = interactable;
        }

        public void SetBuyLabel(string text)
        {
            if (_buyButtonLabel != null)
            {
                _buyButtonLabel.text = text;
            }
        }
    }
}
