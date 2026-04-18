using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Architecture.Services;
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
        private Button _noThanksButton;

        [SerializeField]
        private float _elementDelay = 0.25f;

        [SerializeField]
        private float _noThanksDelay = 2f;

        private ISceneService _sceneService;
        private IUnitPreviewService _unitPreviewService;
        private IAdService _adService;
        private IPlayerProgressService _playerProgressService;
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
            IPlayerProgressService playerProgressService)
        {
            _sceneService = sceneService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
            _playerProgressService = playerProgressService;
        }

        public void Show(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = new CancellationTokenSource();

            _goldEarned = data.GoldEarned;
            _playerProgressService.AddGold(_goldEarned);
            _playerProgressService.Save();
            
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
                _titleText.text = "Победа";
            }
            else
            {
                _titleText.text = "Поражение";
            }
            
            _goldRewardText.text = $"+{data.GoldEarned}";

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
            _unitRewardLabel.text = "Новые бойцы:";

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
                _unitRewardLabel.text = $"{currentKills}/{nextMilestoneKills} убийств до награды";
            }
            else
            {
                _unitRewardLabel.text = "Все награды получены";
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
            _titleText.text = "Победа";
            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+150";
            _unitRewardPanel.SetActive(true);
            _unitRewardLabel.text = "Новые бойцы:";
            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }

        [Button("Preview Victory Progress")]
        private void PreviewVictoryProgress()
        {
            HideAllElements();
            _titlePanel.SetActive(true);
            _titleText.text = "Победа";
            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+75";
            _unitRewardPanel.SetActive(true);
            _unitRewardLabel.text = "8/15 убийств до награды";
            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }

        [Button("Preview Defeat")]
        private void PreviewDefeat()
        {
            HideAllElements();
            _titlePanel.SetActive(true);
            _titleText.text = "Поражение";
            _goldRewardPanel.SetActive(true);
            _goldRewardText.text = "+25";
            _unitRewardPanel.SetActive(true);
            _unitRewardLabel.text = "3/10 убийств до награды";
            _doubleRewardButton.gameObject.SetActive(true);
            _noThanksButton.gameObject.SetActive(true);
        }
#endif
    }
}
