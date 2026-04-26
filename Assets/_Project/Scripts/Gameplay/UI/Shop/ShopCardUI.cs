using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public class ShopCardUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _priceLabel;

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Button _button;

        private Action _onClick;

        private void OnEnable()
        {
            if (_button != null)
                _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        public void Init(string itemName, Sprite icon, string price, Action onClick)
        {
            _nameLabel.text = itemName;
            _onClick = onClick;

            if (_iconImage != null && icon != null)
                _iconImage.sprite = icon;

            if (_priceLabel != null)
                _priceLabel.text = PriceFormat.WithIcon(price);

        }

        private void HandleClick()
        {
            _onClick?.Invoke();
        }
    }
}
