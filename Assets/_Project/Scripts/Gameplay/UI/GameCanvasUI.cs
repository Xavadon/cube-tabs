using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class GameCanvasUI : MonoBehaviour
    {
        private const float SpeedBoostMultiplier = 2f;
        private const float SpeedBoostDuration = 300f;

        [SerializeField]
        private ResultPanelUI _resultPanel;

        [SerializeField]
        private Button _cameraModeButton;

        [SerializeField]
        private Button _surrenderButton;

        [Header("Speed Boost")]
        [SerializeField]
        private Button _speedBoostButton;

        [SerializeField]
        private GameObject _speedBoostTimerPanel;

        [SerializeField]
        private TextMeshProUGUI _speedBoostTimerText;

        private IPlayerProgressService _playerProgressService;
        private LevelConfig _levelConfig;
        private IGameResultService _gameResultService;
        private IAdService _adService;
        private IAudioService _audioService;
        private ITimeScaleService _timeScaleService;
        private ICameraService _cameraService;
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

            _resultPanel.Initialize(sceneService, unitPreviewService, adService, playerProgressService, audioService, localizationService);
            _resultPanel.gameObject.SetActive(false);

            if (_cameraModeButton != null)
            {
                _cameraModeButton.onClick.AddListener(OnCameraModeClicked);
            }

            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.AddListener(OnSurrenderClicked);
            }

            if (_speedBoostButton != null)
            {
                _speedBoostButton.onClick.AddListener(OnSpeedBoostClicked);
            }

            _timeScaleService.OnSpeedBoostEnded += OnSpeedBoostEnded;

            _battleActive = true;
            _timeScaleService.SetSpeedBoostApplied(true);
            RefreshSpeedBoostUI();
        }

        private void OnSpeedBoostEnded()
        {
            RefreshSpeedBoostUI();
        }

        private void OnCameraModeClicked()
        {
            _audioService?.PlayUIClick();
            _cameraService.CycleMode();
        }

        private void OnSurrenderClicked()
        {
            _audioService?.PlayUIClick();
            _gameResultService.Surrender();
        }

        private void OnSpeedBoostClicked()
        {
            _audioService?.PlayUIClick();

            if (_speedBoostButton != null)
            {
                //_speedBoostButton.gameObject.SetActive(false);
                _speedBoostButton.interactable = false;
            }

            if (!_adService.IsRewardedAvailable)
            {
                return;
            }

            _adService.ShowRewarded("SPEED_BOOST", OnSpeedBoostRewardReceived);
        }

        private void OnSpeedBoostRewardReceived(bool success)
        {
            if (success)
            {
                _timeScaleService.StartSpeedBoost(SpeedBoostDuration, SpeedBoostMultiplier);
                RefreshSpeedBoostUI();
            }
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

        private void RefreshSpeedBoostUI()
        {
            float remaining = _timeScaleService.SpeedBoostRemainingSeconds;
            bool boostActive = remaining > 0f;

            if (_speedBoostButton != null)
            {
                _speedBoostButton.interactable = !boostActive;
            }

            if (_speedBoostTimerPanel != null)
            {
                _speedBoostTimerPanel.SetActive(boostActive);
            }

            if (boostActive)
            {
                UpdateTimerText(remaining);
            }
        }

        private void UpdateTimerText(float remaining)
        {
            if (_speedBoostTimerText == null)
                return;

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _speedBoostTimerText.text = $"{minutes}:{seconds:D2}";
        }

        private void OnDisable()
        {
            _battleActive = false;

            if (_timeScaleService != null)
            {
                _timeScaleService.OnSpeedBoostEnded -= OnSpeedBoostEnded;
                _timeScaleService.SetSpeedBoostApplied(false);
            }

            if (_cameraModeButton != null)
            {
                _cameraModeButton.onClick.RemoveListener(OnCameraModeClicked);
            }

            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.RemoveListener(OnSurrenderClicked);
            }

            if (_speedBoostButton != null)
            {
                _speedBoostButton.onClick.RemoveListener(OnSpeedBoostClicked);
            }
        }

        public void ShowResult(GameResultData data)
        {
            _battleActive = false;
            _timeScaleService.SetSpeedBoostApplied(false);

            if (_surrenderButton != null)
                _surrenderButton.gameObject.SetActive(false);

            if (_cameraModeButton != null)
                _cameraModeButton.gameObject.SetActive(false);

            if (_speedBoostButton != null)
                _speedBoostButton.gameObject.SetActive(false);

            if (_speedBoostTimerPanel != null)
                _speedBoostTimerPanel.SetActive(false);

            int currentKills = _playerProgressService.GetLevelKills(_levelConfig.LevelIndex);
            int nextMilestoneKills = GetNextMilestoneKills();

            _resultPanel.Show(data, currentKills, nextMilestoneKills);
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
                    return milestones[i].KillsRequired;
            }

            return 0;
        }
    }
}
