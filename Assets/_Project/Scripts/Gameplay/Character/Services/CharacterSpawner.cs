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
            Debug.Log($"[CharacterSpawner] Service initialized");
            return UniTask.CompletedTask;
        }

        public void Spawn(CharacterType type)
        {
            Character character = _characterFactory.Create(type);

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            
            switch (type)
            {
                case CharacterType.Ally:
                    (position, rotation) = _spawnPointProvider.GetAllySpawn();
                    break;
                case CharacterType.Enemy:
                    (position, rotation) = _spawnPointProvider.GetEnemySpawn();
                    break;
                case CharacterType.None:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            character.transform.position = position;
            character.transform.rotation = rotation;
        }
    }
    
    public interface ICharacterFactory : IService
    {
        Character Create(CharacterType type);
    }

    public class CharacterFactory : ICharacterFactory
    {
        public UniTask Initialize()
        {
            Debug.Log($"[CharacterFactory] Service initialized");
            return UniTask.CompletedTask;
        }

        public Character Create(CharacterType type)
        {
            GameObject characterPrefab = Resources.Load<GameObject>("Prefab/DefaultCharacter");
            GameObject charaGO = Object.Instantiate(characterPrefab);
            
            CharacterData data; 
            
            switch (type)
            {
                case CharacterType.Ally:
                    data = Resources.Load<CharacterData>("Data/Ally DefaultCharacter");
                    charaGO.layer = LayerMask.NameToLayer("Ally");
                    break;
                case CharacterType.Enemy:
                    data = Resources.Load<CharacterData>("Data/Enemy DefaultCharacter");
                    charaGO.layer = LayerMask.NameToLayer("Enemy");
                    break;
                case CharacterType.None:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            
            charaGO.name = data.Name;
      
            if (charaGO.TryGetComponent(out Character character))
            {
                character.Initialize(type, data);
                return character;
            }

            Debug.LogError("[CharacterFactory] Could not get character component");
            return null;
        }
    }
}
