using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Camera;
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

        private static float _speedBoostEndRealtime;

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
        private bool _battleActive;

        public void Initialize(ISceneService sceneService, IPlayerProgressService playerProgressService,
            LevelConfig levelConfig, ICameraService cameraService, IGameResultService gameResultService,
            IUnitPreviewService unitPreviewService, IAdService adService, IAudioService audioService)
        {
            _playerProgressService = playerProgressService;
            _levelConfig = levelConfig;
            _gameResultService = gameResultService;
            _adService = adService;
            _audioService = audioService;

            _resultPanel.Initialize(sceneService, unitPreviewService, adService, playerProgressService, audioService);
            _resultPanel.gameObject.SetActive(false);

            if (_cameraModeButton != null)
            {
                _cameraModeButton.onClick.AddListener(() =>
                {
                    _audioService?.PlayUIClick();
                    cameraService.CycleMode();
                });
            }

            if (_surrenderButton != null)
            {
                _surrenderButton.onClick.AddListener(OnSurrenderClicked);
            }

            if (_speedBoostButton != null)
            {
                _speedBoostButton.onClick.AddListener(OnSpeedBoostClicked);
            }

            _battleActive = true;
            RefreshSpeedBoostUI();

            float remaining = _speedBoostEndRealtime - Time.realtimeSinceStartup;
            Debug.Log($"[GameCanvasUI] Initialize: boost remaining = {remaining:F1}s, timeScale = {Time.timeScale}");
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
                _speedBoostButton.gameObject.SetActive(false);

            if (!_adService.IsRewardedAvailable)
                return;

            Debug.Log("[GameCanvasUI] Showing rewarded ad for speed boost");
            _adService.ShowRewarded("SPEED_BOOST", success =>
            {
                Debug.Log($"[GameCanvasUI] Rewarded callback: success={success}");
                if (success)
                {
                    Debug.Log("[GameCanvasUI] Activating speed boost");
                    _speedBoostEndRealtime = Time.realtimeSinceStartup + SpeedBoostDuration;
                    RefreshSpeedBoostUI();
                }
            });
        }

        private void Update()
        {
            if (!_battleActive)
                return;

            float remaining = _speedBoostEndRealtime - Time.realtimeSinceStartup;

            if (remaining > 0f)
            {
                Time.timeScale = SpeedBoostMultiplier;
                UpdateTimerText(remaining);
            }
            else if (Time.timeScale > 1f)
            {
                Time.timeScale = 1f;
                RefreshSpeedBoostUI();
            }
        }

        private void RefreshSpeedBoostUI()
        {
            float remaining = _speedBoostEndRealtime - Time.realtimeSinceStartup;
            bool boostActive = remaining > 0f;

            if (_speedBoostButton != null)
                _speedBoostButton.gameObject.SetActive(!boostActive);

            if (_speedBoostTimerPanel != null)
                _speedBoostTimerPanel.SetActive(boostActive);

            if (boostActive)
            {
                Time.timeScale = SpeedBoostMultiplier;
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
            Time.timeScale = 1f;

            if (_surrenderButton != null)
                _surrenderButton.onClick.RemoveListener(OnSurrenderClicked);

            if (_speedBoostButton != null)
                _speedBoostButton.onClick.RemoveListener(OnSpeedBoostClicked);
        }

        public void ShowResult(GameResultData data)
        {
            _battleActive = false;
            Time.timeScale = 1f;

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
