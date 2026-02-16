namespace _Project.Scripts.Architecture.Services.Tick
{
    public interface ITickService : IService
    {
        void Register(ITickable tickable);
        void Unregister(ITickable tickable);
    }
}
