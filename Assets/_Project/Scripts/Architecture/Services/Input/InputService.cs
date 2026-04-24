using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Architecture.Services.Input
{
    public interface IInputService : IService
    {
        Vector2 MoveInput { get; }
        Vector2 LookInput { get; }
        bool IsRightMousePressed { get; }
    }

    public class InputService : IInputService
    {
        public Vector2 MoveInput => _playerControls.Player.Move.ReadValue<Vector2>();
        public Vector2 LookInput => _playerControls.Player.Look.ReadValue<Vector2>();
        public bool IsRightMousePressed => Mouse.current != null && Mouse.current.rightButton.isPressed;

        private PlayerControls _playerControls;

        public UniTask Initialize()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _playerControls?.Disable();
            _playerControls?.Dispose();
        }
    }
}