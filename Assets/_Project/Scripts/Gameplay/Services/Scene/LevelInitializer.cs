using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private const string GameCanvasPrefabPath = "Prefab/GameCanvas";

        private readonly ICharacterSpawner _characterSpawner;
        private readonly IGameSessionService _gameSessionService;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IGameResultService _gameResultService;
        private readonly ICameraService _cameraService;
        private readonly IUnitPreviewService _unitPreviewService;
        private readonly IAdService _adService;
        private readonly IAudioService _audioService;
        private readonly IResurrectionService _resurrectionService;
        private readonly ILocalizationService _localizationService;

        private GameCanvasUI _gameCanvasUI;

        public LevelInitializer(
            ICharacterSpawner characterSpawner,
            IGameSessionService gameSessionService,
            IPlayerProgressService playerProgressService,
            IGameResultService gameResultService,
            ICameraService cameraService,
            IUnitPreviewService unitPreviewService,
            IAdService adService,
            IAudioService audioService,
            IResurrectionService resurrectionService,
            ILocalizationService localizationService)
        {
            _characterSpawner = characterSpawner;
            _gameSessionService = gameSessionService;
            _playerProgressService = playerProgressService;
            _gameResultService = gameResultService;
            _cameraService = cameraService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
            _audioService = audioService;
            _resurrectionService = resurrectionService;
            _localizationService = localizationService;
        }

        public UniTask Initialize()
        {
            Debug.Log("[LevelInitializer] Initialized");
            return UniTask.CompletedTask;
        }

        public void InitializeLevel(ISceneService sceneService)
        {
            Debug.Log("[LevelInitializer] Starting level initialization...");

            _unitPreviewService.ClearCache();

            LevelConfig levelConfig = _gameSessionService.SelectedLevel;

            if (levelConfig == null)
            {
                Debug.LogError("[LevelInitializer] No level selected in GameSessionService");
                return;
            }

            var armyUnits = _playerProgressService.ArmyUnits;

            if (armyUnits.Count == 0)
            {
                Debug.LogWarning("[LevelInitializer] No units in army");
            }

            // TODO: Убрать Find — грузить HealthBarPool-префаб из Resources/SO и инстанциировать из кода
            var healthBarPool = Object.FindAnyObjectByType<HealthBarPool>();

            _resurrectionService.SetHealthBarPool(healthBarPool);
            _characterSpawner.SpawnAllies(armyUnits, healthBarPool);

            InitializeGameCanvas(levelConfig, sceneService);

            _gameResultService.StartBattle(levelConfig);

            Debug.Log($"[LevelInitializer] Level '{levelConfig.LevelName}' initialized with {armyUnits.Count} ally units");
        }

        private void InitializeGameCanvas(LevelConfig levelConfig, ISceneService sceneService)
        {
            var canvasPrefab = Resources.Load<GameObject>(GameCanvasPrefabPath);

            if (canvasPrefab == null)
            {
                Debug.LogError($"[LevelInitializer] GameCanvas prefab not found at '{GameCanvasPrefabPath}'");
                return;
            }

            var canvasGO = Object.Instantiate(canvasPrefab);
            _gameCanvasUI = canvasGO.GetComponentInChildren<GameCanvasUI>();

            if (_gameCanvasUI == null)
            {
                Debug.LogError("[LevelInitializer] GameCanvasUI not found on GameCanvas prefab");
                return;
            }

            _gameCanvasUI.Initialize(sceneService, _playerProgressService, levelConfig, _cameraService, _gameResultService, _unitPreviewService, _adService, _audioService, _localizationService);
            _gameResultService.OnGameFinished += _gameCanvasUI.ShowResult;
        }
    }
}
