using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private readonly ICharacterSpawner _characterSpawner;
        private readonly IGameSessionService _gameSessionService;
        private readonly IPlayerProgressService _playerProgressService;

        public LevelInitializer(
            ICharacterSpawner characterSpawner,
            IGameSessionService gameSessionService,
            IPlayerProgressService playerProgressService)
        {
            _characterSpawner = characterSpawner;
            _gameSessionService = gameSessionService;
            _playerProgressService = playerProgressService;
        }

        public UniTask Initialize()
        {
            Debug.Log("[LevelInitializer] Initialized");
            return UniTask.CompletedTask;
        }

        public void InitializeLevel()
        {
            Debug.Log("[LevelInitializer] Starting level initialization...");

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

            _characterSpawner.SpawnFromConfig(levelConfig, armyUnits, healthBarPool);

            Debug.Log($"[LevelInitializer] Level '{levelConfig.LevelName}' initialized with {armyUnits.Count} ally units");
        }
    }
}
