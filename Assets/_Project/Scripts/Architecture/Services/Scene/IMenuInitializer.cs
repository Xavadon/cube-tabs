namespace _Project.Scripts.Architecture.Services.Scene
{
    public interface IMenuInitializer : IService
    {
        void InitializeMenu(ISceneService sceneService);
    }
}
