using System;
using _Project.Scripts.Gameplay.UI.Shop;
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

        [Header("Stats")]
        [SerializeField]
        private TextMeshProUGUI _hpLabel;

        [SerializeField]
        private TextMeshProUGUI _damageLabel;

        [SerializeField]
        private TextMeshProUGUI _speedLabel;

        private Action _onEvolve;

        public void Init(string unitName, int cost, bool canAfford, PreviewHandle preview,
            float hp, float damage, float speed, Action onEvolve)
        {
            _nameLabel.text = unitName;
            _costLabel.text = cost.ToString();

            _hpLabel.text = hp.ToString("0");
            _damageLabel.text = damage.ToString("0");
            _speedLabel.text = speed.ToString("0.#");

            if (_previewImage != null && preview.Texture != null)
                _previewImage.texture = preview.Texture;
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
