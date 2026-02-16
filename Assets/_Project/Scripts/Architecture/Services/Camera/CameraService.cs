using _Project.Scripts.Gameplay.Services.Input;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public class CameraService : ICameraService
    {
        public bool CameraLocked { get; private set; }
        
        private IInputService _inputService;
        private UnityEngine.Camera _playerCamera;

        public CameraService(IInputService inputService)
        {
            _inputService = inputService;
        }

        public UniTask Initialize()
        {
            CameraLocked = false;
            _playerCamera = UnityEngine.Camera.main;
            return UniTask.CompletedTask;
        }
        
        public void Tick(float deltaTime)
        {
            
        }
    }
}
