using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using static _Project.Scripts.Architecture.Services.Localization.LocalizationKeys;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class ResultPanelUI : MonoBehaviour
    {
        private const string RewardedTag = "DOUBLE_REWARD";

        [Header("Title")]
        [SerializeField]
        private GameObject _titlePanel;

        [SerializeField]
        private TextMeshProUGUI _titleText;

        [Header("Gold Reward")]
        [SerializeField]
        private GameObject _goldRewardPanel;

        [SerializeField]
        private TextMeshProUGUI _goldRewardLabel;

        [SerializeField]
        private TextMeshProUGUI _goldRewardText;

        [Header("Unit Reward")]
        [SerializeField]
        private GameObject _unitRewardPanel;

        [SerializeField]
        private TextMeshProUGUI _unitRewardLabel;

        [SerializeField]
        private Transform _unitRewardContent;

        [SerializeField]
        private RewardUnitIconUI _rewardUnitIconPrefab;

        [Header("Buttons")]
        [SerializeField]
        private Button _doubleRewardButton;

        [SerializeField]
        private TextMeshProUGUI _doubleRewardButtonLabel;

        [SerializeField]
        private Button _noThanksButton;

        [SerializeField]
        private TextMeshProUGUI _noThanksButtonLabel;

        [SerializeField]
        private float _elementDelay = 0.25f;

        [SerializeField]
        private float _noThanksDelay = 2f;

        private ISceneService _sceneService;
        private IUnitPreviewService _unitPreviewService;
        private IAdService _adService;
        private IPlayerProgressService _playerProgressService;
        private IAudioService _audioService;
        private ILocalizationService _localization;
        private CancellationTokenSource _showCts;
        private readonly List<RewardUnitIconUI> _spawnedIcons = new();
        private int _goldEarned;

        private void OnEnable()
        {
            _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            _noThanksButton.onClick.AddListener(OnNoThanksClicked);
        }

        private void OnDisable()
        {
            _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            _noThanksButton.onClick.RemoveListener(OnNoThanksClicked);
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = null;
        }

        public void Initialize(
            ISceneService sceneService,
            IUnitPreviewService unitPreviewService,
            IAdService adService,
            IPlayerProgressService playerProgressService,
            IAudioService audioService,
            ILocalizationService localization)
        {
            _sceneService = sceneService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
            _playerProgressService = playerProgressService;
            _audioService = audioService;
            _localization = localization;
        }

        public void Show(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = new CancellationTokenSource();

            _goldEarned = data.GoldEarned;
            
            ClearSpawnedIcons();
            HideAllElements();
            PrepareData(data, currentLevelKills, nextMilestoneKills);

            gameObject.SetActive(true);

            ShowSequenceAsync(_showCts.Token).Forget();
        }

        private void PrepareData(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            if (data.Result == GameResult.Victory)
            {
                _titleText.text = _localization.Get(Result.Victory);
            }
            else
            {
                _titleText.text = _localization.Get(Result.Defeat);
            }

            if (_goldRewardLabel != null)
            {
                _goldRewardLabel.text = _localization.Get(Common.Reward);
            }

            _goldRewardText.text = $"+{data.GoldEarned}";

            if (_doubleRewardButtonLabel != null)
            {
                _doubleRewardButtonLabel.text = _localization.Get(Result.DoubleReward);
            }

            if (_noThanksButtonLabel != null)
            {
                _noThanksButtonLabel.text = _localization.Get(Result.NoThanks);
            }

            bool hasRewards = data.ClaimedMilestones is { Length: > 0 };

            if (hasRewards)
            {
                PrepareUnitRewards(data.ClaimedMilestones);
            }
            else
            {
                PrepareProgress(currentLevelKills, nextMilestoneKills);
            }
        }

        private void PrepareUnitRewards(ClaimedMilestoneData[] milestones)
        {
            _unitRewardLabel.text = _localization.Get(Result.NewUnits);

            foreach (var milestone in milestones)
            {
                if (milestone.Rewards == null)
                {
                    continue;
                }

                foreach (var entry in milestone.Rewards)
                {
                    SpawnRewardIcon(entry.CharacterData, entry.Count);
                }
            }
        }

        private void PrepareProgress(int currentKills, int nextMilestoneKills)
        {
            if (nextMilestoneKills > 0)
            {
                _unitRewardLabel.text = _localization.Get(Result.Progress, currentKills, nextMilestoneKills);
            }
            else
            {
                _unitRewardLabel.text = _localization.Get(Result.AllRewards);
            }
        }

        private async UniTaskVoid ShowSequenceAsync(CancellationToken ct)
        {
            int delayMs = (int)(_elementDelay * 1000);

            _titlePanel.SetActive(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _goldRewardPanel.SetActive(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _unitRewardPanel.SetActive(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            delayMs = (int)(_noThanksDelay * 1000);
            _doubleRewardButton.gameObject.SetActive(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _noThanksButton.gameObject.SetActive(true);
        }

        private void HideAllElements()
        {
            _titlePanel.SetActive(false);
            _goldRewardPanel.SetActive(false);
            _unitRewardPanel.SetActive(false);
            _doubleRewardButton.gameObject.SetActive(false);
            _noThanksButton.gameObject.SetActive(false);
        }

        private void SpawnRewardIcon(Character.Data.CharacterData characterData, int count)
        {
            if (_rewardUnitIconPrefab == null || _unitRewardContent == null)
            {
                return;
            }

            var icon = Instantiate(_rewardUnitIconPrefab, _unitRewardContent);
            _spawnedIcons.Add(icon);

            var portrait = _unitPreviewService.GetPortrait(characterData, 0);
            icon.Setup(portrait, count);
        }

        private void ClearSpawnedIcons()
        {
            foreach (var icon in _spawnedIcons)
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }

            _spawnedIcons.Clear();
        }

        private void OnDoubleRewardClicked()
        {
            _audioService?.PlayUIClick();
            SetButtonsInteractable(false);

            _adService.ShowRewarded(RewardedTag, success =>
            {
                if (success)
                {
                    _playerProgressService.AddGold(_goldEarned);
                    _playerProgressService.Save();
                }

                _sceneService.LoadBootScene();
            });
        }

        private void OnNoThanksClicked()
        {
            _audioService?.PlayUIClick();
            SetButtonsInteractable(false);
            _adService.ShowInterstitial(() => _sceneService.LoadBootScene());
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _doubleRewardButton.interactable = interactable;
            _noThanksButton.interactable = interactable;
        }

#if UNITY_EDITOR
        [Button("Preview Victory with Rewards")]
        private void PreviewVictoryWithRewards()
        {
            HideAllElements();
            _titlePanel.SetActive(true);

            if (_localization != null)
            {
                _titleText.text = _localization.Get(Result.Victory);
            }
            else
            {
                _titleText.text = "Victory";
            }

            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+150";
            _unitRewardPanel.SetActive(true);

            if (_localization != null)
            {
                _unitRewardLabel.text = _localization.Get(Result.NewUnits);
            }
            else
            {
                _unitRewardLabel.text = "New Units:";
            }

            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }

        [Button("Preview Victory Progress")]
        private void PreviewVictoryProgress()
        {
            HideAllElements();
            _titlePanel.SetActive(true);

            if (_localization != null)
            {
                _titleText.text = _localization.Get(Result.Victory);
            }
            else
            {
                _titleText.text = "Victory";
            }

            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+75";
            _unitRewardPanel.SetActive(true);

            if (_localization != null)
            {
                _unitRewardLabel.text = _localization.Get(Result.Progress, 8, 15);
            }
            else
            {
                _unitRewardLabel.text = "8/15 kills to reward";
            }

            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }

        [Button("Preview Defeat")]
        private void PreviewDefeat()
        {
            HideAllElements();
            _titlePanel.SetActive(true);

            if (_localization != null)
            {
                _titleText.text = _localization.Get(Result.Defeat);
            }
            else
            {
                _titleText.text = "Defeat";
            }

            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+25";
            _unitRewardPanel.SetActive(true);

            if (_localization != null)
            {
                _unitRewardLabel.text = _localization.Get(Result.Progress, 3, 10);
            }
            else
            {
                _unitRewardLabel.text = "3/10 kills to reward";
            }

            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }
#endif
    }
}
