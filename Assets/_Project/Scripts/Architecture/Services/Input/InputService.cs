using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Architecture.Services.Input
{
    public interface IInputService : IService
    {
        Vector2 MoveInput { get; }
    }
    
    public class InputService : IInputService
    {
        public Vector2 MoveInput => _playerControls.Player.Move.ReadValue<Vector2>();
        
        private PlayerControls _playerControls;

        public UniTask Initialize()
        {
            _playerControls = new PlayerControls();
            _playerControls.Enable();
            return UniTask.CompletedTask;
        }
    }
}