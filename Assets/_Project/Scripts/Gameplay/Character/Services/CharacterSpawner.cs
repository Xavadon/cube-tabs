using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public enum CharacterType
    {
        Ally,
        Enemy,
        None
    }

    public interface ICharacterSpawner : IService
    {
        void SpawnAllies(List<ResolvedUnit> allyUnits, HealthBarPool healthBarPool);
        void SpawnEnemyWave(SpawnEntry[] entries);
    }

    public class CharacterSpawner : ICharacterSpawner
    {
        private const float InitialDelay = 0.5f;
        private const float SpawnInterval = 0.05f;
        private const float SpawnOffsetRange = 1.5f;

        private readonly ISpawnPointProvider _spawnPointProvider;
        private readonly ICharacterFactory _characterFactory;
        private readonly ICharacterRegistry _characterRegistry;

        private HealthBarPool _healthBarPool;

        public CharacterSpawner(ISpawnPointProvider spawnPointProvider, ICharacterFactory characterFactory, ICharacterRegistry characterRegistry)
        {
            _spawnPointProvider = spawnPointProvider;
            _characterFactory = characterFactory;
            _characterRegistry = characterRegistry;
        }

        public UniTask Initialize()
        {
            Debug.Log("[CharacterSpawner] Initialized");
            return UniTask.CompletedTask;
        }

        public void SpawnAllies(List<ResolvedUnit> allyUnits, HealthBarPool healthBarPool)
        {
            _healthBarPool = healthBarPool;

            var spawnPoints = _spawnPointProvider.GetAllySpawns();
            SpawnAlliesAsync(allyUnits, spawnPoints).Forget();
        }

        public void SpawnEnemyWave(SpawnEntry[] entries)
        {
            var spawnPoints = _spawnPointProvider.GetEnemySpawns();
            SpawnWaveAsync(entries, spawnPoints).Forget();
        }

        private async UniTaskVoid SpawnAlliesAsync(List<ResolvedUnit> units, (Vector3 position, Quaternion rotation)[] spawnPoints)
        {
            Debug.Log($"[TEST]{units == null || units.Count == 0}");
            
            if (units == null || units.Count == 0)
                return;

            //await UniTask.WaitForSeconds(InitialDelay, cancellationToken: ct);

            for (int i = 0; i < units.Count; i++)
            {
                var resolved = units[i];
                PlaceCharacter(
                    _characterFactory.Create(CharacterType.Ally, resolved.Data, resolved.TierIndex),
                    spawnPoints[i % spawnPoints.Length]);

                await UniTask.WaitForSeconds(SpawnInterval);
            }
        }

        private async UniTaskVoid SpawnWaveAsync(SpawnEntry[] entries, (Vector3 position, Quaternion rotation)[] spawnPoints)
        {
            if (entries == null || entries.Length == 0)
                return;

            int spawnIndex = 0;

            foreach (var entry in entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    PlaceCharacter(
                        _characterFactory.Create(CharacterType.Enemy, entry.CharacterData, entry.TierIndex),
                        spawnPoints[spawnIndex % spawnPoints.Length]);

                    spawnIndex++;
                    await UniTask.WaitForSeconds(SpawnInterval);
                }
            }
        }

        private void PlaceCharacter(Character character, (Vector3 position, Quaternion rotation) spawn)
        {
            character.SetRegistry(_characterRegistry);

            if (_healthBarPool != null)
            {
                character.SetHealthBarPool(_healthBarPool);
            }

            _characterRegistry.Register(character);

            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-SpawnOffsetRange, SpawnOffsetRange),
                0f,
                UnityEngine.Random.Range(-SpawnOffsetRange, SpawnOffsetRange));

            character.transform.SetPositionAndRotation(spawn.position + offset, spawn.rotation);
        }
    }

    public interface ICharacterFactory : IService
    {
        Character Create(CharacterType type, CharacterData data, int tierIndex);
    }

    public class CharacterFactory : ICharacterFactory
    {
        private const string CharacterPrefabPath = "Prefab/DefaultCharacter";

        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;

        public CharacterFactory(IInputService inputService, ICameraService cameraService)
        {
            _inputService = inputService;
            _cameraService = cameraService;
        }

        public UniTask Initialize()
        {
            Debug.Log("[CharacterFactory] Initialized");
            return UniTask.CompletedTask;
        }

        public Character Create(CharacterType type, CharacterData data, int tierIndex)
        {
            int layer = type switch
            {
                CharacterType.Ally  => LayerMask.NameToLayer("Ally"),
                CharacterType.Enemy => LayerMask.NameToLayer("Enemy"),
                _                   => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return CreateCharacter(type, data, tierIndex, layer);
        }

        private Character CreateCharacter(CharacterType type, CharacterData data, int tierIndex, int layer)
        {
            GameObject prefab = Resources.Load<GameObject>(CharacterPrefabPath);
            GameObject characterGO = Object.Instantiate(prefab);

            characterGO.name  = data.Name;
            characterGO.layer = layer;
            characterGO.transform.localScale = Vector3.one * data.Scale;

            bool isPlayerControlled = data.GetTier(tierIndex).BrainData is PlayerBrainDataBase;
            IInputService input = isPlayerControlled ? _inputService : null;

            if (characterGO.TryGetComponent(out Character character))
            {
                character.Initialize(type, data, tierIndex, input);

                if (isPlayerControlled)
                    _cameraService.SetTarget(character.transform);

                return character;
            }

            Debug.LogError("[CharacterFactory] Character component not found");
            return null;
        }
    }
}
