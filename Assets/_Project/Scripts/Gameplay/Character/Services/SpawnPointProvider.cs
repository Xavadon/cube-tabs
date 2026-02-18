using _Project.Scripts.Architecture.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface ISpawnPointProvider : IService
    {
        (Vector3 position, Quaternion rotation) GetAllySpawn();
        (Vector3 position, Quaternion rotation) GetEnemySpawn();
    }

    public class SpawnPointProvider : ISpawnPointProvider
    {
        private const string AllySpawnPointTag  = "AllyCharacterSpawnPoint";
        private const string EnemySpawnPointTag = "EnemyCharacterSpawnPoint";

        private Transform _allySpawnPoint;
        private Transform _enemySpawnPoint;

        public UniTask Initialize()
        {
            Debug.Log("[SpawnPointProvider] Initialized");
            return UniTask.CompletedTask;
        }

        private Transform FindSpawnPoint(string tag)
        {
            var go = GameObject.FindGameObjectWithTag(tag);

            if (go != null)
            {
                return go.transform;
            }

            Debug.LogWarning($"[SpawnPointProvider] Spawn point with tag '{tag}' not found");
            return null;
        }

        private (Vector3 position, Quaternion rotation) ToSpawnData(Transform point)
        {
            if (point != null)
            {
                return (point.position, point.rotation);
            }

            return (Vector3.zero, Quaternion.identity);
        }

        public (Vector3 position, Quaternion rotation) GetAllySpawn()
        {
            if (_allySpawnPoint is null)
            {
                _allySpawnPoint = FindSpawnPoint(AllySpawnPointTag);
            }

            return ToSpawnData(_allySpawnPoint);
        }

        public (Vector3 position, Quaternion rotation) GetEnemySpawn()
        {
            if (_enemySpawnPoint is null)
            {
                _enemySpawnPoint = FindSpawnPoint(EnemySpawnPointTag);
            }

            return ToSpawnData(_enemySpawnPoint);
        }
    }
}