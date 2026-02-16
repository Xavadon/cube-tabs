using _Project.Scripts.Architecture.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Input
{
    public interface IInputService : IService
    {
        Vector2 MoveInput { get; }
        bool JumpInput { get; }
        bool RollInput { get; }
        bool SprintInput { get; }

        Vector2 LookInput { get; }
        float ScrollInput { get; }
        bool CameraLockInput { get; }

        bool AttackInput { get; }
        bool BlockInput { get; }

        void SetCursorLocked(bool value);
    }
}
