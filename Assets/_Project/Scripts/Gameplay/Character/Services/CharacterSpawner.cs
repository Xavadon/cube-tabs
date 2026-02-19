using System;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
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
        void Spawn(CharacterType type);
    }

    public class CharacterSpawner : ICharacterSpawner
    {
        private readonly ISpawnPointProvider _spawnPointProvider;
        private readonly ICharacterFactory _characterFactory;

        public CharacterSpawner(ISpawnPointProvider spawnPointProvider, ICharacterFactory characterFactory)
        {
            _spawnPointProvider = spawnPointProvider;
            _characterFactory = characterFactory;
        }

        public UniTask Initialize()
        {
            Debug.Log("[CharacterSpawner] Initialized");
            return UniTask.CompletedTask;
        }

        public void Spawn(CharacterType type)
        {
            Character character = _characterFactory.Create(type);

            (Vector3 position, Quaternion rotation) = type switch
            {
                CharacterType.Ally  => _spawnPointProvider.GetAllySpawn(),
                CharacterType.Enemy => _spawnPointProvider.GetEnemySpawn(),
                _                   => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            character.transform.SetPositionAndRotation(position, rotation);
        }
    }

    public interface ICharacterFactory : IService
    {
        Character Create(CharacterType type);
    }

    public class CharacterFactory : ICharacterFactory
    {
        private const string CharacterPrefabPath = "Prefab/DefaultCharacter";

        public UniTask Initialize()
        {
            Debug.Log("[CharacterFactory] Initialized");
            return UniTask.CompletedTask;
        }

        public Character Create(CharacterType type)
        {
            GameObject prefab = Resources.Load<GameObject>(CharacterPrefabPath);
            GameObject characterGO = Object.Instantiate(prefab);

            (CharacterData data, int layer) = type switch
            {
                CharacterType.Ally  => (Resources.Load<CharacterData>("Data/Ally DefaultCharacter"),  LayerMask.NameToLayer("Ally")),
                CharacterType.Enemy => (Resources.Load<CharacterData>("Data/Enemy DefaultCharacter"), LayerMask.NameToLayer("Enemy")),
                _                   => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

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