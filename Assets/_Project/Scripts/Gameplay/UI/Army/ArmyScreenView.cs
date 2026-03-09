using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Shop;
using TMPro;
using UnityEngine;
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

        [Header("Evolution")]
        [SerializeField]
        private EvolutionPanelUI _evolutionPanel;

        private ArmyScreenPresenter _presenter;
        private readonly List<ArmyUnitCardUI> _cards = new();

        public event Action BuyClicked;
        public event Action TransferClicked;
        public event Action<int> CardClicked;
        public event Action ViewEnabled;

        public EvolutionPanelUI EvolutionPanel => _evolutionPanel;

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog,
            UnitPreviewConfig portraitConfig, UnitPreviewConfig fullBodyConfig)
        {
            _presenter = new ArmyScreenPresenter(this, progress, catalog, portraitConfig, fullBodyConfig);

            _buyButton.onClick.AddListener(OnBuyButtonClicked);
            _transferButton.onClick.AddListener(OnTransferButtonClicked);
        }

        private void OnEnable()
        {
            ViewEnabled?.Invoke();
        }

        private void LateUpdate()
        {
            _presenter?.OnLateUpdate();
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
            _transferButton.onClick.RemoveListener(OnTransferButtonClicked);
            _presenter?.Dispose();
        }

        private void OnBuyButtonClicked()
        {
            BuyClicked?.Invoke();
        }

        private void OnTransferButtonClicked()
        {
            TransferClicked?.Invoke();
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }

        public void ClearCards()
        {
            ClearContainer(_armyContainer);
            ClearContainer(_reserveContainer);
            _cards.Clear();
        }

        public void AddCard(string name, int count, RenderTexture portrait, bool isInArmy)
        {
            var container = isInArmy ? _armyContainer : _reserveContainer;
            var card = Instantiate(_cardPrefab, container);
            int index = _cards.Count;
            card.Init(name, count, portrait, () => CardClicked?.Invoke(index));
            _cards.Add(card);
        }

        public void SetCardSelected(int index, bool selected)
        {
            if (index >= 0 && index < _cards.Count)
                _cards[index].SetSelected(selected);
        }

        public void SetSlotCount(string text)
        {
            _slotCountLabel.text = text;
        }

        public void SetGoldText(string text)
        {
            if (_goldLabel != null)
                _goldLabel.text = text;
        }

        public void SetBuyInteractable(bool interactable)
        {
            _buyButton.interactable = interactable;
        }

        public void SetBuyCost(string text)
        {
            _buyButtonCostLabel.text = text;
        }

        public void SetTransferVisible(bool visible)
        {
            _transferButton.gameObject.SetActive(visible);
        }

        public void SetTransferLabel(string text)
        {
            _transferButtonLabel.text = text;
        }

        public void SetTransferInteractable(bool interactable)
        {
            _transferButton.interactable = interactable;
        }

        public void ShowFullBodyPreview(RenderTexture rt)
        {
            if (_fullBodyPreviewImage == null)
            {
                return;
            }

            _fullBodyPreviewImage.texture = rt;
            _fullBodyPreviewImage.gameObject.SetActive(true);
        }

        public void HideFullBodyPreview()
        {
            if (_fullBodyPreviewImage != null)
            {
                _fullBodyPreviewImage.gameObject.SetActive(false);
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
