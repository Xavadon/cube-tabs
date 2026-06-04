using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GamePush;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private const string LocalizationPath = "Localization";
        private const string FallbackLanguage = "en";

        private readonly Dictionary<string, string> _translations = new();
        private string _currentLanguage;

        public event Action OnLanguageChanged;

        public string CurrentLanguage
        {
            get { return _currentLanguage; }
        }

        public UniTask Initialize()
        {
            string platformLang = GetPlatformLanguage();
            LoadLanguage(platformLang);

            GP_Language.OnChangeLanguage += OnPlatformLanguageChanged;

            Debug.Log($"[LocalizationService] Initialized with language: {_currentLanguage}");
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            GP_Language.OnChangeLanguage -= OnPlatformLanguageChanged;
        }

        public string Get(string key)
        {
            if (_translations.TryGetValue(key, out string value))
            {
                return value;
            }

            Debug.LogWarning($"[LocalizationService] Missing key: {key}");
            return key;
        }

        public string Get(string key, params object[] args)
        {
            string template = Get(key);

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[LocalizationService] Format error for key: {key}");
                return template;
            }
        }

        public void SetLanguage(string languageCode)
        {
            if (_currentLanguage == languageCode)
            {
                return;
            }

            LoadLanguage(languageCode);
            OnLanguageChanged?.Invoke();
        }

        private void OnPlatformLanguageChanged(Language language)
        {
            string code = LanguageToCode(language);
            SetLanguage(code);
        }

        private string GetPlatformLanguage()
        {
            Language language = GP_Language.Current();
            return LanguageToCode(language);
        }

        private static string LanguageToCode(Language language)
        {
            switch (language)
            {
                case Language.Russian:
                    return "ru";
                case Language.English:
                    return "en";
                case Language.Turkish:
                    return "tr";
                case Language.French:
                    return "fr";
                case Language.Italian:
                    return "it";
                case Language.German:
                    return "de";
                case Language.Spanish:
                    return "es";
                case Language.Chineese:
                    return "zh";
                case Language.Portuguese:
                    return "pt";
                case Language.Korean:
                    return "ko";
                case Language.Japanese:
                    return "ja";
                case Language.Arab:
                    return "ar";
                case Language.Hindi:
                    return "hi";
                case Language.Indonesian:
                    return "id";
                default:
                    return "en";
            }
        }

        private void LoadLanguage(string languageCode)
        {
            TextAsset asset = Resources.Load<TextAsset>($"{LocalizationPath}/{languageCode}");

            if (asset == null)
            {
                Debug.LogWarning($"[LocalizationService] Language '{languageCode}' not found, falling back to '{FallbackLanguage}'");
                asset = Resources.Load<TextAsset>($"{LocalizationPath}/{FallbackLanguage}");
            }

            if (asset == null)
            {
                Debug.LogError($"[LocalizationService] Fallback language '{FallbackLanguage}' not found");
                _currentLanguage = languageCode;
                return;
            }

            ParseJson(asset.text);
            _currentLanguage = languageCode;
        }

        private void ParseJson(string json)
        {
            _translations.Clear();

            LocalizationData wrapper = JsonUtility.FromJson<LocalizationData>(json);

            if (wrapper == null || wrapper.entries == null)
            {
                return;
            }

            foreach (LocalizationEntry entry in wrapper.entries)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                if (entry.value != null)
                {
                    _translations[entry.key] = entry.value;
                }
                else
                {
                    _translations[entry.key] = string.Empty;
                }
            }
        }

        [Serializable]
        private class LocalizationData
        {
            public LocalizationEntry[] entries;
        }

        [Serializable]
        private class LocalizationEntry
        {
            public string key;
            public string value;
        }
    }
}
