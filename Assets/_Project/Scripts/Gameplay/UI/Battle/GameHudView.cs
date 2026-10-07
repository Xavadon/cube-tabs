using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using _Project.Scripts.Gameplay.UI.Kit;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Battle
{
    [RequireComponent(typeof(UIDocument))]
    public class GameHudView : MonoBehaviour
    {
        private const float SpeedBoostMultiplier = 2f;
        private const float SpeedBoostDuration = 300f;
        private const float SecondsPerMinute = 60f;
        private const string SpeedBoostTag = "SPEED_BOOST";

        private const string HudName = "hud";
        private const string ResultName = "result";
        private const string SurrenderButtonName = "surrender-button";
        private const string CameraButtonName = "camera-button";
        private const string CameraGesturesName = "camera-gestures";
        private const string SpeedButtonName = "speed-button";
        private const string SpeedTimerName = "speed-timer";
        private const string SpeedTimerLabelName = "speed-timer-label";
        private const string WaveBannerName = "wave-banner";
        private const string WaveTitleName = "wave-title";
        private const string WaveSubtitleName = "wave-subtitle";

        [SerializeField]
        private UIDocument _document;

        [Header("Wave Banner")]
        [SerializeField]
        private float _waveBannerStartScale = 1.6f;

        [SerializeField]
        private float _waveBannerPopDuration = 0.35f;

        [SerializeField]
        private float _waveBannerHoldDuration = 2f;

        [SerializeField]
        private float _waveBannerFadeDuration = 0.5f;

        [Header("Result")]
        [SerializeField]
        private float _resultElementDelay = 0.25f;

        [SerializeField]
        private float _resultNoThanksDelay = 2f;

        private ILocalizationService _localizationService;
        private IPlayerProgressService _playerProgressService;
        private LevelConfig _levelConfig;
        private IGameResultService _gameResultService;
        private IAdService _adService;
        private IAudioService _audioService;
        private ITimeScaleService _timeScaleService;
        private ICameraService _cameraService;
        private ResultPanel _resultPanel;
        private CameraGestures _cameraGestures;

        private VisualElement _hud;
        private Button _surrenderButton;
        private Button _cameraModeButton;
        private Button _speedBoostButton;
        private VisualElement _speedBoostTimer;
        private Label _speedBoostTimerLabel;
        private VisualElement _waveBanner;
        private Label _waveTitle;
        private Label _waveSubtitle;

        private Sequence _waveBannerSequence;
        private float _waveBannerScale;
        private float _waveBannerAlpha;
        private bool _battleActive;

        public void Initialize(ISceneService sceneService, IPlayerProgressService playerProgressService,
            LevelConfig levelConfig, ICameraService cameraService, IGameResultService gameResultService,
            IUnitPreviewService unitPreviewService, IAdService adService, IAudioService audioService,
            ILocalizationService localizationService, ITimeScaleService timeScaleService)
        {
            _playerProgressService = playerProgressService;
            _levelConfig = levelConfig;
            _gameResultService = gameResultService;
            _adService = adService;
            _audioService = audioService;
            _timeScaleService = timeScaleService;
            _cameraService = cameraService;
            _localizationService = localizationService;

            var root = _document.rootVisualElement;
            _hud = root.Q(HudName);
            _surrenderButton = _hud.Q<Button>(SurrenderButtonName);
            _cameraModeButton = _hud.Q<Button>(CameraButtonName);
            _speedBoostButton = _hud.Q<Button>(SpeedButtonName);
            _speedBoostTimer = _hud.Q(SpeedTimerName);
            _speedBoostTimerLabel = _hud.Q<Label>(SpeedTimerLabelName);
            _waveBanner = _hud.Q(WaveBannerName);
            _waveTitle = _hud.Q<Label>(WaveTitleName);
            _waveSubtitle = _hud.Q<Label>(WaveSubtitleName);
            _cameraGestures = new CameraGestures(_hud.Q(CameraGesturesName), cameraService);

            _resultPanel = new ResultPanel(root.Q(ResultName), sceneService, unitPreviewService, adService,
                playerProgressService, audioService, localizationService, _resultElementDelay, _resultNoThanksDelay);

            _cameraModeButton.clicked += OnCameraModeClicked;
            _surrenderButton.clicked += OnSurrenderClicked;
            _speedBoostButton.clicked += OnSpeedBoostClicked;

            HideWaveBanner();
            _gameResultService.OnWaveStarted += OnWaveStarted;
            _timeScaleService.OnSpeedBoostEnded += OnSpeedBoostEnded;

            _battleActive = true;
            _timeScaleService.SetSpeedBoostApplied(true);
            RefreshSpeedBoostUI();
        }

        public void ShowResult(GameResultData data)
        {
            _battleActive = false;
            HideWaveBanner();
            _timeScaleService.SetSpeedBoostApplied(false);
            _hud.SetDisplayed(false);

            int currentKills = _playerProgressService.GetLevelKills(_levelConfig.LevelIndex);
            _resultPanel.Show(data, currentKills, GetNextMilestoneKills());
        }

        private void Update()
        {
            if (!_battleActive)
            {
                return;
            }

            float remaining = _timeScaleService.SpeedBoostRemainingSeconds;
            if (remaining > 0f)
            {
                UpdateTimerText(remaining);
            }
        }

        private void OnDisable()
        {
            _battleActive = false;
            _waveBannerSequence?.Kill();
            _waveBannerSequence = null;

            if (_gameResultService != null)
            {
                _gameResultService.OnWaveStarted -= OnWaveStarted;
            }

            if (_timeScaleService != null)
            {
                _timeScaleService.OnSpeedBoostEnded -= OnSpeedBoostEnded;
                _timeScaleService.SetSpeedBoostApplied(false);
            }

            if (_hud != null)
            {
                _cameraModeButton.clicked -= OnCameraModeClicked;
                _surrenderButton.clicked -= OnSurrenderClicked;
                _speedBoostButton.clicked -= OnSpeedBoostClicked;
            }

            _resultPanel?.Dispose();
            _cameraGestures?.Dispose();
        }

        private void OnSpeedBoostEnded()
        {
            RefreshSpeedBoostUI();
        }

        private void OnCameraModeClicked()
        {
            _audioService.PlayUIClick();
            _cameraService.CycleMode();
        }

        private void OnSurrenderClicked()
        {
            _audioService.PlayUIClick();
            _gameResultService.Surrender();
        }

        private void OnSpeedBoostClicked()
        {
            _audioService.PlayUIClick();

            if (!_adService.IsRewardedAvailable)
            {
                return;
            }

            _speedBoostButton.SetEnabled(false);
            _adService.ShowRewarded(SpeedBoostTag, OnSpeedBoostRewardReceived);
        }

        private void OnSpeedBoostRewardReceived(bool success)
        {
            if (success)
            {
                _timeScaleService.StartSpeedBoost(SpeedBoostDuration, SpeedBoostMultiplier);
            }

            RefreshSpeedBoostUI();
        }

        private void RefreshSpeedBoostUI()
        {
            float remaining = _timeScaleService.SpeedBoostRemainingSeconds;
            bool boostActive = remaining > 0f;

            _speedBoostButton.SetEnabled(!boostActive);
            _speedBoostTimer.SetDisplayed(boostActive);

            if (boostActive)
            {
                UpdateTimerText(remaining);
            }
        }

        private void UpdateTimerText(float remaining)
        {
            int minutes = Mathf.FloorToInt(remaining / SecondsPerMinute);
            int seconds = Mathf.FloorToInt(remaining % SecondsPerMinute);
            _speedBoostTimerLabel.text = $"{minutes}:{seconds:D2}";
        }

        private void OnWaveStarted(int waveIndex, int waveCount)
        {
            bool isFinalWave = waveCount > 1 && waveIndex == waveCount - 1;

            if (isFinalWave)
            {
                _waveTitle.text = _localizationService.Get(LocalizationKeys.Battle.FinalWave);
            }
            else
            {
                _waveTitle.text = _localizationService.Get(LocalizationKeys.Battle.Wave, waveIndex + 1, waveCount);
            }

            string announcementKey = _levelConfig.Waves[waveIndex].AnnouncementKey;
            bool hasAnnouncement = !string.IsNullOrEmpty(announcementKey);
            _waveSubtitle.SetDisplayed(hasAnnouncement);

            if (hasAnnouncement)
            {
                _waveSubtitle.text = _localizationService.Get(announcementKey);
            }

            PlayWaveBanner();
        }

        private void PlayWaveBanner()
        {
            _waveBannerSequence?.Kill();

            SetWaveBannerScale(_waveBannerStartScale);
            SetWaveBannerAlpha(1f);
            _waveBanner.SetDisplayed(true);

            _waveBannerSequence = DOTween.Sequence()
                .Append(DOTween.To(GetWaveBannerScale, SetWaveBannerScale, 1f, _waveBannerPopDuration).SetEase(Ease.OutBack))
                .AppendInterval(_waveBannerHoldDuration)
                .Append(DOTween.To(GetWaveBannerAlpha, SetWaveBannerAlpha, 0f, _waveBannerFadeDuration))
                .OnComplete(HideWaveBanner)
                .SetUpdate(true);
        }

        private float GetWaveBannerScale()
        {
            return _waveBannerScale;
        }

        private void SetWaveBannerScale(float scale)
        {
            _waveBannerScale = scale;
            _waveBanner.style.scale = new Scale(new Vector2(scale, scale));
        }

        private float GetWaveBannerAlpha()
        {
            return _waveBannerAlpha;
        }

        private void SetWaveBannerAlpha(float alpha)
        {
            _waveBannerAlpha = alpha;
            _waveBanner.style.opacity = alpha;
        }

        private void HideWaveBanner()
        {
            _waveBannerSequence?.Kill();
            _waveBannerSequence = null;
            _waveBanner.SetDisplayed(false);
        }

        private int GetNextMilestoneKills()
        {
            var milestones = _levelConfig.Milestones;
            if (milestones == null || milestones.Length == 0)
            {
                return 0;
            }

            int levelIndex = _levelConfig.LevelIndex;
            for (int i = 0; i < milestones.Length; i++)
            {
                if (!_playerProgressService.IsMilestoneClaimed(levelIndex, i))
                {
                    return milestones[i].KillsRequired;
                }
            }

            return 0;
        }
    }
}
