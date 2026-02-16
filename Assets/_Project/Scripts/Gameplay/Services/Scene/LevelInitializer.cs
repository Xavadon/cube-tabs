using _Project.Scripts.Architecture.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        /*private readonly PlayerSpawner _playerSpawner;
        private readonly CameraSpawner _cameraSpawner;*/

        /*public LevelInitializer(PlayerSpawner playerSpawner, CameraSpawner cameraSpawner)
        {
            _playerSpawner = playerSpawner;
            _cameraSpawner = cameraSpawner;
        }*/
        
        public UniTask Initialize()
        {
            Debug.Log("[LevelInitializer] Initialized");
            return UniTask.CompletedTask;
        }
        
        public void InitializeLevel()
        {
            /*Debug.Log("[LevelInitializer] Starting level initialization...");
            GameObject player = _playerSpawner.SpawnPlayer();

            if (player == null)
            {
                Debug.LogError("[LevelInitializer] Failed to spawn player!");
                return;
            }

            Transform cameraTarget = FindCameraTarget(player.transform);

            if (cameraTarget == null)
            {
                Debug.LogWarning("[LevelInitializer] CameraTarget not found on player, using player root transform");
                cameraTarget = player.transform;
            }

            _cameraSpawner.SpawnCamera(cameraTarget);
            */
            
            Debug.Log("[LevelInitializer] Level initialized successfully");
        }

        
        private Transform FindCameraTarget(Transform playerRoot)
        {
            Transform[] children = playerRoot.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.CompareTag("CameraTarget"))
                {
                    return child;
                }
            }

            Transform cameraTarget = playerRoot.Find("CameraTarget");
            if (cameraTarget != null)
            {
                return cameraTarget;
            }

            return null;
        }
    }
}
