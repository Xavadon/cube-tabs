using _Project.Scripts.Architecture;
using UnityEngine;

namespace _Project.Scripts
{
    public class BootStrap : MonoBehaviour
    {
        private static BootStrap _instance;

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
