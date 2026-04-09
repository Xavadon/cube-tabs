using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class ResultPanelUI : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField]
        private Image _sun;

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
        
        private float _sunRotationDuration = 10f;

        private ISceneService _sceneService;
        private IUnitPreviewService _unitPreviewService;
        private Tween _sunRotationTween;
        private CancellationTokenSource _showCts;
        private readonly List<RewardUnitIconUI> _spawnedIcons = new();

        private void OnEnable()
        {
            _doubleRewardButton.onClick.AddListener(OnButtonClicked);
            _noThanksButton.onClick.AddListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            _doubleRewardButton.onClick.RemoveListener(OnButtonClicked);
            _noThanksButton.onClick.RemoveListener(OnButtonClicked);
            _sunRotationTween?.Kill();
            _sunRotationTween = null;
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = null;
        }

        public void Initialize(ISceneService sceneService, IUnitPreviewService unitPreviewService)
        {
            _sceneService = sceneService;
            _unitPreviewService = unitPreviewService;
        }

        public void Show(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = new CancellationTokenSource();

            ClearSpawnedIcons();
            HideAllElements();
            PrepareData(data, currentLevelKills, nextMilestoneKills);

            gameObject.SetActive(true);
            StartSunRotation();

            ShowSequenceAsync(_showCts.Token).Forget();
        }

        private void PrepareData(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            _titleText.text = data.Result == GameResult.Victory ? "Победа" : "Поражение";
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
                    continue;

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

            _sun.gameObject.SetActive(true);
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
            _sun.gameObject.SetActive(false);
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

        private void StartSunRotation()
        {
            if (_sun == null)
            {
                return;
            }

            _sunRotationTween?.Kill();
            _sun.transform.rotation = Quaternion.identity;
            _sunRotationTween = _sun.transform
                .DORotate(new Vector3(0, 0, -360), _sunRotationDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }

        private void OnButtonClicked()
        {
            // TODO: Implement ad reward doubling for _doubleRewardButton
            _sceneService.LoadBootScene();
        }

#if UNITY_EDITOR
        [Button("Preview Victory with Rewards")]
        private void PreviewVictoryWithRewards()
        {
            HideAllElements();
            _sun.gameObject.SetActive(true);
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
            _sun.gameObject.SetActive(true);
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
            _sun.gameObject.SetActive(true);
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
