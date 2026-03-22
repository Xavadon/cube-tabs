using System;
using System.Collections.Generic;
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
            SpawnCharacters(allyUnits, CharacterType.Ally, _spawnPointProvider.GetAllySpawns());
        }

        public void SpawnEnemyWave(SpawnEntry[] entries)
        {
            SpawnGroup(entries, CharacterType.Enemy, _spawnPointProvider.GetEnemySpawns());
        }

        private void SpawnCharacters(List<ResolvedUnit> units, CharacterType type, (Vector3 position, Quaternion rotation)[] spawnPoints)
        {
            if (units == null || units.Count == 0)
                return;

            for (int i = 0; i < units.Count; i++)
            {
                var resolved = units[i];
                Character character = _characterFactory.Create(type, resolved.Data, resolved.TierIndex);
                character.SetRegistry(_characterRegistry);
                if (_healthBarPool != null)
                    character.SetHealthBarPool(_healthBarPool);
                _characterRegistry.Register(character);

                var (position, rotation) = spawnPoints[i % spawnPoints.Length];

                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-1.5f, 1.5f),
                    0f,
                    UnityEngine.Random.Range(-1.5f, 1.5f)
                );

                character.transform.SetPositionAndRotation(position + offset, rotation);
            }
        }

        private void SpawnGroup(SpawnEntry[] entries, CharacterType type, (Vector3 position, Quaternion rotation)[] spawnPoints)
        {
            if (entries == null || entries.Length == 0)
                return;

            int spawnIndex = 0;

            foreach (var entry in entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    Character character = _characterFactory.Create(type, entry.CharacterData, entry.TierIndex);
                    character.SetRegistry(_characterRegistry);
                    if (_healthBarPool != null)
                        character.SetHealthBarPool(_healthBarPool);
                    _characterRegistry.Register(character);

                    var (position, rotation) = spawnPoints[spawnIndex % spawnPoints.Length];

                    Vector3 offset = new Vector3(
                        UnityEngine.Random.Range(-1.5f, 1.5f),
                        0f,
                        UnityEngine.Random.Range(-1.5f, 1.5f)
                    );

                    character.transform.SetPositionAndRotation(position + offset, rotation);
                    spawnIndex++;
                }
            }
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
