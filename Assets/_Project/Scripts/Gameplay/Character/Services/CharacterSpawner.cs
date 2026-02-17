using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            var character = _characterFactory.Create(type);

            var (position, rotation) = _spawnPointProvider.GetCharacterSpawn();
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
            CharacterData data = Resources.Load<CharacterData>("Data/DefaultCharacter");
            
            GameObject charaGO = Object.Instantiate(characterPrefab);
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
