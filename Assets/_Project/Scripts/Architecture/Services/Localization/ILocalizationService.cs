using System;

namespace _Project.Scripts.Architecture.Services.Localization
{
    public interface ILocalizationService : IService
    {
        event Action OnLanguageChanged;
        string CurrentLanguage { get; }
        string Get(string key);
        string Get(string key, params object[] args);
        void SetLanguage(string languageCode);
    }
}
