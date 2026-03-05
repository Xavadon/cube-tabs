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
        private Button _button;

        private Action _onClick;

        public void Init(string unitName, int count, Action onClick)
        {
            _nameLabel.text = unitName;
            _countLabel.text = count > 1 ? $"x{count}" : "";
            _onClick = onClick;
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
