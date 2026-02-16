using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class SpawnPointProvider : ISpawnPointProvider
    {
        private const string PlayerSpawnPointTag = "PlayerSpawnPoint";

        public UniTask Initialize()
        {
            Debug.Log("[SpawnPointProvider] Initialized");
            return UniTask.CompletedTask;
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag(PlayerSpawnPointTag);

            if (spawnPoint != null)
            {
                return spawnPoint.transform.position;
            }

            Debug.LogWarning($"[SpawnPointProvider] GameObject with tag '{PlayerSpawnPointTag}' not found, using default position (0,0,0)");
            return Vector3.zero;
        }

        public Quaternion GetPlayerSpawnRotation()
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag(PlayerSpawnPointTag);

            if (spawnPoint != null)
            {
                return spawnPoint.transform.rotation;
            }

            Debug.LogWarning($"[SpawnPointProvider] GameObject with tag '{PlayerSpawnPointTag}' not found, using default rotation");
            return Quaternion.identity;
        }
    }
}
