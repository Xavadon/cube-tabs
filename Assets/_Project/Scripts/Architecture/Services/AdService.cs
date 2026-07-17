using System;
using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using GamePush;

namespace _Project.Scripts.Architecture.Services
{
    public class AdService : IAdService
    {
        private readonly IPlayerProgressService _progress;
        private readonly ITimeScaleService _timeScaleService;

#if UNITY_EDITOR
        public bool IsInterstitialAvailable => true;
        public bool IsRewardedAvailable => true;
#else
        public bool IsInterstitialAvailable => !_progress.NoAds && GP_Ads.IsFullscreenAvailable();
        public bool IsRewardedAvailable => GP_Ads.IsRewardedAvailable();
#endif

        public AdService(IPlayerProgressService progress, ITimeScaleService timeScaleService)
        {
            _progress = progress;
            _timeScaleService = timeScaleService;
        }

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void ShowInterstitial(Action onComplete = null)
        {
            if (_progress.NoAds)
            {
                onComplete?.Invoke();
                return;
            }

#if UNITY_EDITOR
            onComplete?.Invoke();
#else
            PauseGame();
            GP_Ads.ShowFullscreen(
                onFullscreenStart: null,
                onFullscreenClose: _ =>
                {
                    ResumeGame();
                    onComplete?.Invoke();
                }
            );
#endif
        }
        
        public void ShowRewarded(string tag, Action<bool> onComplete)
        {
#if UNITY_EDITOR
            onComplete?.Invoke(true);
#else
            PauseGame();
            GP_Ads.ShowRewarded(
                idOrTag: tag,
                onRewardedReward: null,
                onRewardedStart: null,
                onRewardedClose: success =>
                {
                    ResumeGame();
                    onComplete?.Invoke(success);
                }
            );
#endif
        }

        private void PauseGame()
        {
            _timeScaleService.Pause();
            GP_Game.Pause();
        }

        private void ResumeGame()
        {
            _timeScaleService.Resume();
            GP_Game.Resume();
        }
    }
}
