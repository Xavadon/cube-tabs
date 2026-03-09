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

            gameObject.SetActive(false);
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
                var rt = _previewFactory.CreatePreview(unit, i);
                card.Init(unit.Name, unit.Price, _progress.GetOwnedCount(unit), _progress.CanAfford(unit.Price),
                    () => OnBuyUnit(unit), rt);
                _cards.Add((card, unit));
            }
        }

        private void OnBuyUnit(CharacterData unit)
        {
            _progress.BuyUnit(unit);
        }

        private void HandleUpgradeSlot()
        {
            _progress.UpgradeArmySlots();
        }

        private void RefreshAll()
        {
            _goldLabel.text = _progress.Gold.ToString();

            foreach (var (card, data) in _cards)
                card.Refresh(_progress.GetOwnedCount(data), _progress.CanAfford(data.Price));

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
