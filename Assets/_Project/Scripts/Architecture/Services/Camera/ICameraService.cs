using _Project.Scripts.Architecture.Services.Tick;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public enum CameraMode { TopDown, Free, ThirdPerson, FirstPerson }

    public interface ICameraService : IService, ITickable
    {
        bool CameraLocked { get; }
        CameraMode CurrentMode { get; }
        void SetTarget(Transform target);
        void CycleMode();
        void ResetToDefault();
        void AddTrauma(float amount, Vector3 sourcePosition);
        void Pan(Vector2 screenDelta);
        void Zoom(float factor);
        void ResetUserView();
    }
}
