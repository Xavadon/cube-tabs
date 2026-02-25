using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class LevelPointUI : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private TextMeshProUGUI _label;

        private Action _onClick;

        public void Init(string levelName, Action onClick)
        {
            _label.text = levelName;
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
