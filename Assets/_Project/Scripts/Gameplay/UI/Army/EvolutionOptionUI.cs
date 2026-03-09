using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class EvolutionOptionUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _costLabel;

        [SerializeField]
        private RawImage _previewImage;

        [SerializeField]
        private Button _button;

        private Action _onEvolve;

        public void Init(string unitName, int cost, bool canAfford, RenderTexture portrait, Action onEvolve)
        {
            _nameLabel.text = unitName;
            _costLabel.text = cost.ToString();

            if (_previewImage != null && portrait != null)
                _previewImage.texture = portrait;
            _button.interactable = canAfford;
            _onEvolve = onEvolve;
            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            _onEvolve?.Invoke();
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }
}
