using System;
using Cysharp.Threading.Tasks;
using GamePush;

namespace _Project.Scripts.Architecture.Services
{
    public class AdService : IAdService
    {
#if UNITY_EDITOR
        public bool IsInterstitialAvailable => true;
        public bool IsRewardedAvailable => true;
#else
        public bool IsInterstitialAvailable => GP_Ads.IsFullscreenAvailable();
        public bool IsRewardedAvailable => GP_Ads.IsRewardedAvailable();
#endif

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void ShowInterstitial(Action onComplete = null)
        {
#if UNITY_EDITOR
            onComplete?.Invoke();
#else
            GP_Ads.ShowFullscreen(
                onFullscreenStart: null,
                onFullscreenClose: _ => onComplete?.Invoke()
            );
#endif
        }

        public void ShowRewarded(string tag, Action<bool> onComplete)
        {
#if UNITY_EDITOR
            onComplete?.Invoke(true);
#else
            var rewarded = false;

            GP_Ads.ShowRewarded(
                idOrTag: tag,
                onRewardedReward: _ => rewarded = true,
                onRewardedStart: null,
                onRewardedClose: _ => onComplete?.Invoke(rewarded)
            );
#endif
        }
    }
}
