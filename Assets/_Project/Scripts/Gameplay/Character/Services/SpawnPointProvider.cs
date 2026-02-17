using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface ISpawnPointProvider : IService
    {
        (Vector3 position, Quaternion rotation) GetCharacterSpawn();
    }
    
    public class SpawnPointProvider : ISpawnPointProvider
    {
        private const string CharacterSpawnPointTag = "CharacterSpawnPoint";
    
        private Transform _playerSpawnPoint;

        public UniTask Initialize()
        {
            Debug.Log("[SpawnPointProvider] Initialized");
            return UniTask.CompletedTask;
        }

        private void TryFindSpawnPoint()
        {
            var go = GameObject.FindGameObjectWithTag(CharacterSpawnPointTag);

            if (go != null)
            {
                _playerSpawnPoint = go.transform;
            }
            else
            {
                Debug.LogWarning($"[SpawnPointProvider] '{CharacterSpawnPointTag}' not found");
            }
        }

        public (Vector3 position, Quaternion rotation) GetCharacterSpawn()
        {
            if (_playerSpawnPoint == null)
            {
                TryFindSpawnPoint();
            }
            
            if (_playerSpawnPoint != null)
            {
                return (_playerSpawnPoint.position, _playerSpawnPoint.rotation);
            }
        
            return (Vector3.zero, Quaternion.identity);
        }
    }
}