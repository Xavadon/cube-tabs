using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private const string LevelConfigPath = "Data/LevelConfig";

        private readonly ICharacterSpawner _characterSpawner;

        public LevelInitializer(ICharacterSpawner characterSpawner)
        {
            _characterSpawner = characterSpawner;
        }

        public UniTask Initialize()
        {
            Debug.Log("[LevelInitializer] Initialized");
            return UniTask.CompletedTask;
        }

        public void InitializeLevel()
        {
            Debug.Log("[LevelInitializer] Starting level initialization...");

            var config = Resources.Load<LevelConfig>(LevelConfigPath);

            if (config == null)
            {
                Debug.LogError($"[LevelInitializer] LevelConfig not found at '{LevelConfigPath}'");
                return;
            }

            // TODO: Убрать Find — грузить HealthBarPool-префаб из Resources/SO и инстанциировать из кода
            var healthBarPool = Object.FindAnyObjectByType<HealthBarPool>();
            _characterSpawner.SpawnFromConfig(config, healthBarPool);

            Debug.Log("[LevelInitializer] Level initialized successfully");
        }
    }
}
