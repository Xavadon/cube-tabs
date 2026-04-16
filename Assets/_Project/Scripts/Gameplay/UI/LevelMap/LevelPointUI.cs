using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    public class LevelPointUI : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        private Action _onClick;

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        public void Initialize(Action onClick)
        {
            _onClick = onClick;
        }

        private void HandleClick()
        {
            _onClick?.Invoke();
        }
    }
}
