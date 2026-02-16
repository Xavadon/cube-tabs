using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Architecture.Services.Scene
{
    public class SceneService : IService
    {
        private readonly ILevelInitializer _levelInitializer;

        public SceneService(ILevelInitializer levelInitializer)
        {
            _levelInitializer = levelInitializer;
        }
        
        public UniTask Initialize()
        {
            Debug.Log("[SceneService] Initialized");
            return UniTask.CompletedTask;
        }

        public async UniTask LoadSceneAsync(string name)
        {
            Debug.Log($"[SceneService] Loading scene: {name}");
            await SceneManager.LoadSceneAsync(name).ToUniTask();
            Debug.Log($"[SceneService] Scene loaded: {name}");
        }
        
        public async UniTask LoadGameScene()
        {
            await LoadSceneAsync("Game");
            _levelInitializer.InitializeLevel();

            Debug.Log("[GameSceneManager] Game scene loaded and initialized");
        }

        public async UniTask LoadDemoLevelScene()
        {
            await LoadSceneAsync("DemoLevel");
            _levelInitializer.InitializeLevel();

            Debug.Log("[GameSceneManager] DemoLevel scene loaded and initialized");
        }
    }
}
