using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopScreenUI : MonoBehaviour
    {
        [SerializeField]
        private ShopUnitCardUI _cardPrefab;

        [SerializeField]
        private Transform _cardsContainer;

        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [Header("Slot Upgrade")]
        [SerializeField]
        private Button _upgradeSlotButton;

        [SerializeField]
        private TextMeshProUGUI _slotInfoLabel;

        [SerializeField]
        private TextMeshProUGUI _slotCostLabel;

        private IPlayerProgressService _progress;
        private ShopCatalog _catalog;
        private readonly List<(ShopUnitCardUI card, CharacterData data)> _cards = new();
        private UnitPreviewFactory _previewFactory;

        public void Initialize(ShopCatalog catalog, IPlayerProgressService progress, UnitPreviewConfig previewConfig)
        {
            gameObject.SetActive(false);
            
            return;
            
            _progress = progress;
            _catalog = catalog;
            _previewFactory = new UnitPreviewFactory(previewConfig);

            _progress.OnGoldChanged += RefreshAll;
            _progress.OnOwnedChanged += RefreshAll;
            _progress.OnArmyChanged += RefreshSlotInfo;
            _upgradeSlotButton.onClick.AddListener(HandleUpgradeSlot);

            BuildCards();
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshAll;
                _progress.OnOwnedChanged -= RefreshAll;
                _progress.OnArmyChanged -= RefreshSlotInfo;
            }

            _upgradeSlotButton.onClick.RemoveListener(HandleUpgradeSlot);
            _previewFactory?.Dispose();
        }

        private void BuildCards()
        {
            for (int i = 0; i < _catalog.AvailableUnits.Length; i++)
            {
                var unit = _catalog.AvailableUnits[i];
                var card = Instantiate(_cardPrefab, _cardsContainer);
                var handle = _previewFactory.CreatePreview(unit, 0, i);
                card.Init(unit.Name, unit.Price, 0, _progress.CanAfford(unit.Price),
                    () => _progress.BuyBaseUnit(), handle.Texture);
                _cards.Add((card, unit));
            }
        }

        private void HandleUpgradeSlot()
        {
            _progress.UpgradeArmySlots();
        }

        private void RefreshAll()
        {
            _goldLabel.text = _progress.Gold.ToString();

            foreach (var (card, data) in _cards)
                card.Refresh(0, _progress.CanAfford(data.Price));

            RefreshSlotInfo();
        }

        private void RefreshSlotInfo()
        {
            _slotInfoLabel.text = $"{_progress.ArmySlots}/{_catalog.MaxArmySlots}";

            int upgradeCost = _progress.GetSlotUpgradeCost();
            bool maxed = _progress.ArmySlots >= _catalog.MaxArmySlots;
            _slotCostLabel.text = maxed ? "MAX" : upgradeCost.ToString();
            _upgradeSlotButton.interactable = !maxed && _progress.CanAfford(upgradeCost);
        }
    }
}
