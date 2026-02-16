using _Project.Scripts.Architecture.Services.Tick;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public interface ICameraService : IService, ITickable
    {
        bool CameraLocked { get; }
    }
}
