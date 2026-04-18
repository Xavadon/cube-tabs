using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Army;
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
        private readonly ICameraService _cameraService;
        private readonly IGameSessionService _gameSessionService;

        public SceneService(
            ILevelInitializer levelInitializer,
            IMenuInitializer menuInitializer,
            IPlayerProgressService playerProgressService,
            ICameraService cameraService,
            IGameSessionService gameSessionService)
        {
            _levelInitializer = levelInitializer;
            _menuInitializer = menuInitializer;
            _playerProgressService = playerProgressService;
            _cameraService = cameraService;
            _gameSessionService = gameSessionService;
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
            ArmyScreenController.ResetGoldRewardCooldown();
            await LoadSceneAsync("Boot");
        }
        
        public async UniTask LoadGameScene()
        {
            string sceneName = _gameSessionService.SelectedLevel?.SceneName ?? "Game";
            await LoadSceneAsync(sceneName);
            _levelInitializer.InitializeLevel(this);

            Debug.Log($"[SceneService] Game scene '{sceneName}' loaded and initialized");
        }

        public async UniTask LoadMenuScene()
        {
            _playerProgressService.Save();
            _cameraService.ResetToDefault();
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
