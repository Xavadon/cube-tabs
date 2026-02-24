using System;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
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
        void SpawnFromConfig(LevelConfig config);
    }

    public class CharacterSpawner : ICharacterSpawner
    {
        private readonly ISpawnPointProvider _spawnPointProvider;
        private readonly ICharacterFactory _characterFactory;
        private readonly ICharacterRegistry _characterRegistry;

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

        public void SpawnFromConfig(LevelConfig config)
        {
            SpawnGroup(config.Enemies, CharacterType.Enemy, _spawnPointProvider.GetEnemySpawns());
            SpawnGroup(config.Allies, CharacterType.Ally, _spawnPointProvider.GetAllySpawns());
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
                    Character character = _characterFactory.Create(type, entry.CharacterData);
                    character.SetRegistry(_characterRegistry);
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
        Character Create(CharacterType type, CharacterData data);
    }

    public class CharacterFactory : ICharacterFactory
    {
        private const string CharacterPrefabPath = "Prefab/DefaultCharacter";

        public UniTask Initialize()
        {
            Debug.Log("[CharacterFactory] Initialized");
            return UniTask.CompletedTask;
        }

        public Character Create(CharacterType type, CharacterData data)
        {
            int layer = type switch
            {
                CharacterType.Ally  => LayerMask.NameToLayer("Ally"),
                CharacterType.Enemy => LayerMask.NameToLayer("Enemy"),
                _                   => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            return CreateCharacter(type, data, layer);
        }

        private Character CreateCharacter(CharacterType type, CharacterData data, int layer)
        {
            GameObject prefab = Resources.Load<GameObject>(CharacterPrefabPath);
            GameObject characterGO = Object.Instantiate(prefab);

            characterGO.name  = data.Name;
            characterGO.layer = layer;

            if (characterGO.TryGetComponent(out Character character))
            {
                character.Initialize(type, data);
                return character;
            }

            Debug.LogError("[CharacterFactory] Character component not found");
            return null;
        }
    }
}
