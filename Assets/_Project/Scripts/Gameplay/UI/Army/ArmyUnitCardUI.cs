using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyUnitCardUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _countLabel;

        [SerializeField]
        private RawImage _previewImage;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Color _normalColor = new(0.2f, 0.2f, 0.2f, 1f);

        [SerializeField]
        private Color _selectedColor = new(0.4f, 0.6f, 1f, 1f);

        private Action _onClick;

        public void Init(string unitName, int count, RenderTexture portrait, Action onClick)
        {
            _nameLabel.text = unitName;

            if (count > 1)
            {
                _nameLabel.text = $"{unitName} x{count}";
                _countLabel.text = $"{count}";
            }
            else if (_countLabel != null)
            {
                _countLabel.text = "";
            }

            _onClick = onClick;

            if (_previewImage != null && portrait != null)
                _previewImage.texture = portrait;

            _button.onClick.AddListener(HandleClick);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_background != null)
                _background.color = selected ? _selectedColor : _normalColor;
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
