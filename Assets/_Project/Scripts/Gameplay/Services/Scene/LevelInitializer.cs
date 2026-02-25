using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private const string ArmyConfigPath = "Data/ArmyConfig";

        private readonly ICharacterSpawner _characterSpawner;
        private readonly IGameSessionService _gameSessionService;

        public LevelInitializer(ICharacterSpawner characterSpawner, IGameSessionService gameSessionService)
        {
            _characterSpawner = characterSpawner;
            _gameSessionService = gameSessionService;
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

            var armyConfig = Resources.Load<ArmyConfig>(ArmyConfigPath);

            if (armyConfig == null)
            {
                Debug.LogError($"[LevelInitializer] ArmyConfig not found at '{ArmyConfigPath}'");
                return;
            }

            // TODO: Убрать Find — грузить HealthBarPool-префаб из Resources/SO и инстанциировать из кода
            var healthBarPool = Object.FindAnyObjectByType<HealthBarPool>();

            _characterSpawner.SpawnFromConfig(levelConfig, armyConfig, healthBarPool);

            Debug.Log($"[LevelInitializer] Level '{levelConfig.LevelName}' initialized successfully");
        }
    }
}
