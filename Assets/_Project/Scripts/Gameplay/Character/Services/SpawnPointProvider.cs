using _Project.Scripts.Architecture.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface ISpawnPointProvider : IService
    {
        (Vector3 position, Quaternion rotation)[] GetAllySpawns();
        (Vector3 position, Quaternion rotation)[] GetEnemySpawns();
    }

    public class SpawnPointProvider : ISpawnPointProvider
    {
        private const string AllySpawnPointTag  = "AllyCharacterSpawnPoint";
        private const string EnemySpawnPointTag = "EnemyCharacterSpawnPoint";

        private Transform[] _allySpawnPoints;
        private Transform[] _enemySpawnPoints;

        public UniTask Initialize()
        {
            Debug.Log("[SpawnPointProvider] Initialized");
            return UniTask.CompletedTask;
        }

        public (Vector3 position, Quaternion rotation)[] GetAllySpawns()
        {
            return GetSpawnPoints(AllySpawnPointTag, ref _allySpawnPoints);
        }

        public (Vector3 position, Quaternion rotation)[] GetEnemySpawns()
        {
            return GetSpawnPoints(EnemySpawnPointTag, ref _enemySpawnPoints);
        }

        private (Vector3 position, Quaternion rotation)[] GetSpawnPoints(string tag, ref Transform[] cache)
        {
            if (cache == null)
            {
                var objects = GameObject.FindGameObjectsWithTag(tag);

                if (objects.Length == 0)
                {
                    Debug.LogWarning($"[SpawnPointProvider] No spawn points with tag '{tag}' found");
                    return new[] { (Vector3.zero, Quaternion.identity) };
                }

                cache = new Transform[objects.Length];
                for (int i = 0; i < objects.Length; i++)
                    cache[i] = objects[i].transform;
            }

            var result = new (Vector3, Quaternion)[cache.Length];
            for (int i = 0; i < cache.Length; i++)
                result[i] = (cache[i].position, cache[i].rotation);

            return result;
        }
    }
}
