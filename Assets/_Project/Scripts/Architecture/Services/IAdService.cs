using System;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Architecture.Services
{
    public interface IAdService : IService
    {
        bool IsInterstitialAvailable { get; }
        bool IsRewardedAvailable { get; }

        void ShowInterstitial(Action onComplete = null);
        void ShowRewarded(string tag, Action<bool> onComplete);
    }
}
