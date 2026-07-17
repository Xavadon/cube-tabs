using System;
using System.Threading;
using _Project.Scripts.Architecture.Services.Tick;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public interface ITimeScaleService : IService, ITickable
    {
        bool IsPaused { get; }
        float SpeedMultiplier { get; }
        float SpeedBoostRemainingSeconds { get; }
        event Action OnSpeedBoostEnded;
        void Pause();
        void Resume();
        void StartSpeedBoost(float duration, float multiplier);
        void StopSpeedBoost();
    }

    public class TimeScaleService : ITimeScaleService
    {
        private CancellationTokenSource _boostCts;
        private float _boostEndRealtime;

        public bool IsPaused { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float SpeedBoostRemainingSeconds => Mathf.Max(0f, _boostEndRealtime - Time.realtimeSinceStartup);
        public event Action OnSpeedBoostEnded;

        public UniTask Initialize() => UniTask.CompletedTask;

        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            Debug.Log("[TimeScaleService] Paused");
        }

        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            ApplyTimeScale();
            Debug.Log($"[TimeScaleService] Resumed, timeScale={SpeedMultiplier}");
        }

        public void StartSpeedBoost(float duration, float multiplier)
        {
            _boostCts?.Cancel();
            _boostCts = new CancellationTokenSource();

            SpeedMultiplier = multiplier;
            _boostEndRealtime = Time.realtimeSinceStartup + duration;

            if (!IsPaused)
            {
                ApplyTimeScale();
            }

            Debug.Log($"[TimeScaleService] SpeedBoost started: {multiplier}x for {duration}s");
            RunBoostTimerAsync(_boostCts.Token).Forget();
        }

        public void StopSpeedBoost()
        {
            _boostCts?.Cancel();
            _boostCts = null;
            _boostEndRealtime = 0f;

            SpeedMultiplier = 1f;
            if (!IsPaused)
            {
                ApplyTimeScale();
            }

            Debug.Log("[TimeScaleService] SpeedBoost stopped");
        }

        private async UniTaskVoid RunBoostTimerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (SpeedBoostRemainingSeconds <= 0f)
                {
                    break;
                }
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            SpeedMultiplier = 1f;
            _boostEndRealtime = 0f;

            if (!IsPaused)
            {
                ApplyTimeScale();
            }

            Debug.Log("[TimeScaleService] SpeedBoost ended");
            OnSpeedBoostEnded?.Invoke();
        }

        public void Tick(float deltaTime)
        {
#if UNITY_EDITOR
            if (IsPaused)
            {
                return;
            }

            if (UnityEngine.Input.GetKey(KeyCode.Equals) || UnityEngine.Input.GetKey(KeyCode.Mouse4))
            {
                Time.timeScale = 4f;
            }
            else if (UnityEngine.Input.GetKey(KeyCode.Minus))
            {
                Time.timeScale = 0.25f;
            }
            else
            {
                ApplyTimeScale();
            }
#endif
        }

        public void Dispose()
        {
            _boostCts?.Cancel();
            Time.timeScale = 1f;
        }

        private void ApplyTimeScale()
        {
            Time.timeScale = SpeedMultiplier;
        }
    }
}
