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
        private Button _button;

        private Action _onEvolve;

        public void Init(string unitName, int cost, bool canAfford, Action onEvolve)
        {
            _nameLabel.text = unitName;
            _costLabel.text = cost.ToString();
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
