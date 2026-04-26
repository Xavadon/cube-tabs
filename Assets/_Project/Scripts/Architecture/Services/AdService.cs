using System;
using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services
{
    public class AdService : IAdService
    {
        private readonly IPlayerProgressService _progress;

#if UNITY_EDITOR
        public bool IsInterstitialAvailable => true;
        public bool IsRewardedAvailable => true;
#else
        public bool IsInterstitialAvailable => !_progress.NoAds && GP_Ads.IsFullscreenAvailable();
        public bool IsRewardedAvailable => GP_Ads.IsRewardedAvailable();
#endif

        public AdService(IPlayerProgressService progress)
        {
            _progress = progress;
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
            Debug.Log($"[AdService] ShowRewarded: tag={tag}");

            GP_Ads.ShowRewarded(
                idOrTag: tag,
                onRewardedReward: null,
                onRewardedStart: null,
                onRewardedClose: success =>
                {
                    Debug.Log($"[AdService] onRewardedClose: success={success}");
                    onComplete?.Invoke(success);
                }
            );
#endif
        }
    }
}
