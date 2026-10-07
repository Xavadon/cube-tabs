using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Gameplay.Services
{
    public interface IGameSessionService : IService
    {
        LevelConfig SelectedLevel { get; }
        void SelectLevel(LevelConfig level);
    }

    public class GameSessionService : IGameSessionService
    {
        public LevelConfig SelectedLevel { get; private set; }

        public void SelectLevel(LevelConfig level)
        {
            SelectedLevel = level;
        }

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }
    }
}
