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
        bool IsSpeedBoostActive { get; }
        event Action OnSpeedBoostEnded;
        void Pause();
        void Resume();
        void StartSpeedBoost(float duration, float multiplier);
        void StopSpeedBoost();
        void SetSpeedBoostApplied(bool applied);
    }

    public class TimeScaleService : ITimeScaleService
    {
        private CancellationTokenSource _boostCts;

        // Каждый ран заканчивается пересозданием контейнера (ResultPanel -> LoadBootScene -> Project.Initialize),
        // поэтому буст живёт в статике: Time.realtimeSinceStartup обнуляется только с рестартом приложения.
        private static float _boostEndRealtime;
        private static float _boostMultiplier = 1f;

        // Ускорение живёт между ранами: таймер идёт в реальном времени всегда,
        // но сам timeScale множится только в бою (в меню и на экране результата — 1x).
        private bool _boostApplied;

        public bool IsPaused { get; private set; }
        public float SpeedMultiplier => IsSpeedBoostActive && _boostApplied ? _boostMultiplier : 1f;
        public float SpeedBoostRemainingSeconds => Mathf.Max(0f, _boostEndRealtime - Time.realtimeSinceStartup);
        public bool IsSpeedBoostActive => SpeedBoostRemainingSeconds > 0f;
        public event Action OnSpeedBoostEnded;

        public UniTask Initialize()
        {
            if (IsSpeedBoostActive)
            {
                _boostCts = new CancellationTokenSource();
                RunBoostTimerAsync(_boostCts.Token).Forget();
                Debug.Log($"[TimeScaleService] SpeedBoost restored: {_boostMultiplier}x, {SpeedBoostRemainingSeconds:F0}s left");
            }

            return UniTask.CompletedTask;
        }

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

            _boostMultiplier = multiplier;
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
            _boostMultiplier = 1f;

            if (!IsPaused)
            {
                ApplyTimeScale();
            }

            Debug.Log("[TimeScaleService] SpeedBoost stopped");
        }

        public void SetSpeedBoostApplied(bool applied)
        {
            if (_boostApplied == applied)
            {
                return;
            }

            _boostApplied = applied;

            if (!IsPaused)
            {
                ApplyTimeScale();
            }
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

            _boostMultiplier = 1f;
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
