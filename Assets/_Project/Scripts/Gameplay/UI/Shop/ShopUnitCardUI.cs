using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopUnitCardUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _priceLabel;

        [SerializeField]
        private TextMeshProUGUI _ownedCountLabel;

        [SerializeField]
        private Button _buyButton;

        private Action _onBuy;

        public void Init(string unitName, int price, int ownedCount, bool canAfford, Action onBuy)
        {
            _nameLabel.text = unitName;
            _priceLabel.text = price.ToString();
            _onBuy = onBuy;

            _buyButton.onClick.AddListener(HandleBuy);

            Refresh(ownedCount, canAfford);
        }

        public void Refresh(int ownedCount, bool canAfford)
        {
            _ownedCountLabel.text = ownedCount > 0 ? $"x{ownedCount}" : "";
            _buyButton.interactable = canAfford;
        }

        private void HandleBuy()
        {
            _onBuy?.Invoke();
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(HandleBuy);
        }
    }
}
