using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Architecture.Services.Scene
{
    public interface ISceneService : IService
    {
        UniTask LoadBootScene();
        UniTask LoadGameScene();
        UniTask LoadMenuScene();
        UniTask LoadDemoLevelScene();
    }
    
    public class SceneService : ISceneService
    {
        private readonly ILevelInitializer _levelInitializer;
        private readonly IMenuInitializer _menuInitializer;
        private readonly IPlayerProgressService _playerProgressService;

        public SceneService(
            ILevelInitializer levelInitializer,
            IMenuInitializer menuInitializer,
            IPlayerProgressService playerProgressService)
        {
            _levelInitializer = levelInitializer;
            _menuInitializer = menuInitializer;
            _playerProgressService = playerProgressService;
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

        public async UniTask LoadBootScene()
        {
            _playerProgressService.Save();
            await LoadSceneAsync("Boot");
        }
        
        public async UniTask LoadGameScene()
        {
            await LoadSceneAsync("Game");
            _levelInitializer.InitializeLevel(this);

            Debug.Log("[GameSceneManager] Game scene loaded and initialized");
        }

        public async UniTask LoadMenuScene()
        {
            _playerProgressService.Save();
            await LoadSceneAsync("Menu");
            _menuInitializer.InitializeMenu(this);
            Debug.Log("[SceneService] Menu scene loaded and initialized");
        }

        public async UniTask LoadDemoLevelScene()
        {
            await LoadSceneAsync("DemoLevel");
            _levelInitializer.InitializeLevel(this);

            Debug.Log("[GameSceneManager] DemoLevel scene loaded and initialized");
        }
    }
}
