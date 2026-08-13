using _Project.Scripts.Architecture;
using _Project.Scripts.Dev;
using UnityEngine;

namespace _Project.Scripts
{
    public class BootStrap : MonoBehaviour
    {
        private static BootStrap _instance;

        [SerializeField]
        private bool _showResetSaveButton = true;

        private async void Awake()
        {
            if (_instance != null)
            {
                var old = _instance;
                _instance = null;
                Project.Dispose();
                Destroy(old.gameObject);
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            await Project.Initialize();

            // Тестовая кнопка. Работает и в обычном WebGL-билде — снять галку перед релизом.
            if (_showResetSaveButton)
                gameObject.AddComponent<DevResetSaveButton>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                Project.Dispose();
            }
        }
    }
}
