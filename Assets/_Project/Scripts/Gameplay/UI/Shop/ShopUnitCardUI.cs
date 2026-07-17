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
        private RawImage _previewImage;

        [SerializeField]
        private Button _button;

        private Action _onClick;

        public void Init(string unitName, RenderTexture portrait, string price, bool canAfford, Action onClick)
        {
            _nameLabel.text = unitName;

            if (_priceLabel != null)
                _priceLabel.text = price;

            _onClick = onClick;
            _button.interactable = canAfford;

            if (_previewImage != null && portrait != null)
                _previewImage.texture = portrait;

            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            _onClick?.Invoke();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }
}
