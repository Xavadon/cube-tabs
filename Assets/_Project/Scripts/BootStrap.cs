using _Project.Scripts.Architecture;
using UnityEngine;

namespace _Project.Scripts
{
    public class BootStrap : MonoBehaviour
    {
        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);
            await Project.Initialize();
        }

        private void OnDestroy()
        {
            Project.Dispose();
        }
    }
}
