using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.UI.Battle;
using Cysharp.Threading.Tasks;
using UnityEngine;
using _Project.Scripts.Gameplay.Services.CombatFeedback;
using _Project.Scripts.Gameplay.UI.CombatFeedback;
using _Project.Scripts.Gameplay.UI.Settings;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private const string GameCanvasPrefabPath = "Prefab/GameCanvas";
        private const string HudPrefabPath = "Prefab/UI/Game";
        private const string SettingsPrefabPath = "Prefab/UI/Settings";

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
        private readonly ITimeScaleService _timeScaleService;
        private readonly ICombatFeedbackService _combatFeedbackService;

        private GameHudView _hud;

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
            ILocalizationService localizationService,
            ITimeScaleService timeScaleService,
            ICombatFeedbackService combatFeedbackService)
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
            _timeScaleService = timeScaleService;
            _combatFeedbackService = combatFeedbackService;
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

            HealthBarPool healthBarPool = InitializeGameCanvas();

            if (healthBarPool == null)
            {
                return;
            }

            _resurrectionService.SetHealthBarPool(healthBarPool);
            _characterSpawner.SpawnAllies(armyUnits, healthBarPool);

            InitializeHud(levelConfig, sceneService);
            InitializeSettings();

            _gameResultService.StartBattle(levelConfig);
            _audioService.PlayMusic(MusicTheme.Battle);

            Debug.Log($"[LevelInitializer] Level '{levelConfig.LevelName}' initialized with {armyUnits.Count} ally units");
        }

        private void InitializeHud(LevelConfig levelConfig, ISceneService sceneService)
        {
            var hudPrefab = Resources.Load<GameHudView>(HudPrefabPath);

            if (hudPrefab == null)
            {
                Debug.LogError($"[LevelInitializer] Game HUD prefab not found at '{HudPrefabPath}'");
                return;
            }

            _hud = Object.Instantiate(hudPrefab);
            _hud.Initialize(sceneService, _playerProgressService, levelConfig, _cameraService, _gameResultService, _unitPreviewService, _adService, _audioService, _localizationService, _timeScaleService);
            _gameResultService.OnGameFinished += _hud.ShowResult;
        }

        private HealthBarPool InitializeGameCanvas()
        {
            var canvasPrefab = Resources.Load<GameObject>(GameCanvasPrefabPath);

            if (canvasPrefab == null)
            {
                Debug.LogError($"[LevelInitializer] GameCanvas prefab not found at '{GameCanvasPrefabPath}'");
                return null;
            }

            var canvasGO = Object.Instantiate(canvasPrefab);
            var floatingTextPool = canvasGO.GetComponentInChildren<FloatingTextPool>(true);

            if (floatingTextPool == null)
            {
                Debug.LogError("[LevelInitializer] FloatingTextPool not found on GameCanvas prefab");
            }
            else
            {
                _combatFeedbackService.BindView(floatingTextPool);
            }

            var healthBarPool = canvasGO.GetComponentInChildren<HealthBarPool>(true);

            if (healthBarPool == null)
            {
                Debug.LogError("[LevelInitializer] HealthBarPool not found on GameCanvas prefab");
            }

            return healthBarPool;
        }

        private void InitializeSettings()
        {
            var settingsPrefab = Resources.Load<SettingsView>(SettingsPrefabPath);

            if (settingsPrefab == null)
            {
                Debug.LogError($"[LevelInitializer] Settings prefab not found at '{SettingsPrefabPath}'");
                return;
            }

            Object.Instantiate(settingsPrefab).Initialize(_audioService, _localizationService);
        }
    }
}
