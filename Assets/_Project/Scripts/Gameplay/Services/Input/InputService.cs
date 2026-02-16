using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Gameplay.Services.Input
{
    public class InputService : IInputService
    {
        public Vector2 MoveInput => _playerControls.Player.Move.ReadValue<Vector2>();
        public bool JumpInput => _playerControls.Player.Jump.triggered;
        public bool RollInput => _playerControls.Player.Roll.triggered;
        public bool SprintInput => _playerControls.Player.Sprint.IsPressed();

        public Vector2 LookInput => _playerControls.Player.Look.ReadValue<Vector2>();
        public float ScrollInput => Mouse.current.scroll.ReadValue().y * -0.1f;
        public bool CameraLockInput => _playerControls.Player.LockCamera.triggered;

        public bool AttackInput => _playerControls.Player.Fire.triggered;
        public bool BlockInput => _playerControls.Player.Block.IsPressed();
        
        private PlayerControls _playerControls;

        public UniTask Initialize()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();
            return UniTask.CompletedTask;
        }

        public void SetCursorLocked(bool value)
        {
            if (value)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }
}
