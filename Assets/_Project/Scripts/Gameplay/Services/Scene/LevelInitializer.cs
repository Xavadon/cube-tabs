using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class LevelInitializer : ILevelInitializer
    {
        private readonly CharacterSpawner _characterSpawner;

        public LevelInitializer(CharacterSpawner characterSpawner)
        {
            _characterSpawner = characterSpawner;
        }
        
        public UniTask Initialize()
        {
            Debug.Log("[LevelInitializer] Initialized");
            return UniTask.CompletedTask;
        }
        
        public void InitializeLevel()
        {
            Debug.Log("[LevelInitializer] Starting level initialization...");

            _characterSpawner.Spawn(CharacterType.Enemy);
            
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
