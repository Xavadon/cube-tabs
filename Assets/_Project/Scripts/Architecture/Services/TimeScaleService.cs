using _Project.Scripts.Architecture.Services.Tick;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public interface ITimeScaleService : IService, ITickable
    {
        bool IsTimeSpeedUp { get; }
    }

    public class TimeScaleService : ITimeScaleService
    {
        public bool IsTimeSpeedUp { get; private set; }

        public UniTask Initialize() => UniTask.CompletedTask;

        public void Tick(float deltaTime)
        {
#if UNITY_EDITOR
            if (UnityEngine.Input.GetKey(KeyCode.Equals) || UnityEngine.Input.GetKey(KeyCode.Mouse4))
            {
                IsTimeSpeedUp = true;
                Time.timeScale = 4;
            }
            else if (UnityEngine.Input.GetKey(KeyCode.Minus))
            {
                IsTimeSpeedUp = false;
                Time.timeScale = 0.25f;
            }
            else
            {
                IsTimeSpeedUp = false;
                Time.timeScale = 1;
            }
#endif
        }

        public void Dispose()
        {
            Time.timeScale = 1;
        }
    }
}
