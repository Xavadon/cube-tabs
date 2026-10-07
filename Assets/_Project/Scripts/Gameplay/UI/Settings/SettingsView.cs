using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Settings
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsView : MonoBehaviour
    {
        private const string OpenButtonName = "open-button";
        private const string CloseButtonName = "close-button";
        private const string OverlayName = "overlay";
        private const string TitleName = "title";
        private const string SoundLabelName = "sound-label";
        private const string MusicLabelName = "music-label";
        private const string SoundSliderName = "sound-slider";
        private const string MusicSliderName = "music-slider";

        [SerializeField]
        private UIDocument _document;

        private IAudioService _audioService;
        private ILocalizationService _localization;
        private Button _openButton;
        private Button _closeButton;
        private VisualElement _overlay;
        private Slider _soundSlider;
        private Slider _musicSlider;

        public void Initialize(IAudioService audioService, ILocalizationService localizationService)
        {
            _audioService = audioService;
            _localization = localizationService;
            _localization.OnLanguageChanged += UpdateLocalizedTexts;

            var root = _document.rootVisualElement;
            _openButton = root.Q<Button>(OpenButtonName);
            _closeButton = root.Q<Button>(CloseButtonName);
            _overlay = root.Q<VisualElement>(OverlayName);
            _soundSlider = root.Q<Slider>(SoundSliderName);
            _musicSlider = root.Q<Slider>(MusicSliderName);

            UpdateLocalizedTexts();

            _openButton.clicked += Open;
            _closeButton.clicked += Close;
            _soundSlider.RegisterValueChangedCallback(OnSoundChanged);
            _musicSlider.RegisterValueChangedCallback(OnMusicChanged);

            _overlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (_localization != null)
            {
                _localization.OnLanguageChanged -= UpdateLocalizedTexts;
            }

            if (_openButton == null)
            {
                return;
            }

            _openButton.clicked -= Open;
            _closeButton.clicked -= Close;
            _soundSlider.UnregisterValueChangedCallback(OnSoundChanged);
            _musicSlider.UnregisterValueChangedCallback(OnMusicChanged);
        }

        private void Open()
        {
            _audioService.PlayUIClick();
            _soundSlider.SetValueWithoutNotify(_audioService.SoundVolume);
            _musicSlider.SetValueWithoutNotify(_audioService.MusicVolume);
            _overlay.style.display = DisplayStyle.Flex;
        }

        private void UpdateLocalizedTexts()
        {
            var root = _document.rootVisualElement;
            root.Q<Label>(TitleName).text = _localization.Get(LocalizationKeys.Settings.Title);
            root.Q<Label>(SoundLabelName).text = _localization.Get(LocalizationKeys.Settings.Sound);
            root.Q<Label>(MusicLabelName).text = _localization.Get(LocalizationKeys.Settings.Music);
        }

        private void Close()
        {
            _audioService.PlayUIClick();
            _audioService.SaveVolumeSettings();
            _overlay.style.display = DisplayStyle.None;
        }

        private void OnSoundChanged(ChangeEvent<float> evt)
        {
            _audioService.SetSoundVolume(evt.newValue);
        }

        private void OnMusicChanged(ChangeEvent<float> evt)
        {
            _audioService.SetMusicVolume(evt.newValue);
        }
    }
}
