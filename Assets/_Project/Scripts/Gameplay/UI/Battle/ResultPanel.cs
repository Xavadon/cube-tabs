using System;
using System.Threading;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Kit;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;
using static _Project.Scripts.Architecture.Services.Localization.LocalizationKeys;

namespace _Project.Scripts.Gameplay.UI.Battle
{
    public class ResultPanel : IDisposable
    {
        private const string RewardedTag = "DOUBLE_REWARD";

        private const string SunName = "result-sun";
        private const string TitleName = "result-title";
        private const string GoldName = "result-gold";
        private const string GoldValueName = "result-gold-value";
        private const string UnitsName = "result-units";
        private const string TaskLabelName = "result-task-label";
        private const string SlotsLabelName = "result-slots-label";
        private const string UnitsLabelName = "result-units-label";
        private const string UnitsListName = "result-units-list";
        private const string DoubleRewardButtonName = "double-reward-button";
        private const string NoThanksButtonName = "no-thanks-button";
        private const string RestartButtonName = "restart-button";

        private const string RewardUnitClass = "reward-unit";
        private const string RewardUnitCountClass = "reward-unit__count";
        private const string PortraitClass = "portrait";

        private const float SunTurnDegrees = -360f;
        private const float SunTurnDuration = 10f;
        private const float PulseScale = 1.06f;
        private const float PulseDuration = 0.6f;
        private const int MillisecondsPerSecond = 1000;

        private readonly VisualElement _root;
        private readonly VisualElement _sun;
        private readonly Label _title;
        private readonly VisualElement _gold;
        private readonly Label _goldValue;
        private readonly VisualElement _units;
        private readonly Label _taskLabel;
        private readonly Label _slotsLabel;
        private readonly Label _unitsLabel;
        private readonly VisualElement _unitsList;
        private readonly Button _doubleRewardButton;
        private readonly Button _noThanksButton;
        private readonly Button _restartButton;

        private readonly ISceneService _sceneService;
        private readonly IUnitPreviewService _unitPreviewService;
        private readonly IAdService _adService;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IAudioService _audioService;
        private readonly ILocalizationService _localization;
        private readonly float _elementDelay;
        private readonly float _noThanksDelay;

        private CancellationTokenSource _showCts;
        private Tween _sunTween;
        private Tween _pulseTween;
        private float _sunAngle;
        private float _pulse = 1f;
        private int _goldEarned;

        public ResultPanel(VisualElement root, ISceneService sceneService, IUnitPreviewService unitPreviewService,
            IAdService adService, IPlayerProgressService playerProgressService, IAudioService audioService,
            ILocalizationService localization, float elementDelay, float noThanksDelay)
        {
            _root = root;
            _sceneService = sceneService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
            _playerProgressService = playerProgressService;
            _audioService = audioService;
            _localization = localization;
            _elementDelay = elementDelay;
            _noThanksDelay = noThanksDelay;

            UILocalization.Apply(root, localization);

            _sun = root.Q(SunName);
            _title = root.Q<Label>(TitleName);
            _gold = root.Q(GoldName);
            _goldValue = root.Q<Label>(GoldValueName);
            _units = root.Q(UnitsName);
            _taskLabel = root.Q<Label>(TaskLabelName);
            _slotsLabel = root.Q<Label>(SlotsLabelName);
            _unitsLabel = root.Q<Label>(UnitsLabelName);
            _unitsList = root.Q(UnitsListName);
            _doubleRewardButton = root.Q<Button>(DoubleRewardButtonName);
            _noThanksButton = root.Q<Button>(NoThanksButtonName);
            _restartButton = root.Q<Button>(RestartButtonName);

            _doubleRewardButton.clicked += OnDoubleRewardClicked;
            _noThanksButton.clicked += OnNoThanksClicked;
            _restartButton.clicked += OnRestartClicked;

            _root.SetDisplayed(false);
        }

        public void Show(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            CancelShowSequence();
            _showCts = new CancellationTokenSource();
            _goldEarned = data.GoldEarned;

            HideAllElements();
            PrepareData(data, currentLevelKills, nextMilestoneKills);
            _root.SetDisplayed(true);
            PlayLoops();
            ShowSequenceAsync(_showCts.Token).Forget();
        }

        public void Dispose()
        {
            CancelShowSequence();
            _sunTween?.Kill();
            _pulseTween?.Kill();
            _doubleRewardButton.clicked -= OnDoubleRewardClicked;
            _noThanksButton.clicked -= OnNoThanksClicked;
            _restartButton.clicked -= OnRestartClicked;
        }

        private void CancelShowSequence()
        {
            _showCts?.Cancel();
            _showCts?.Dispose();
            _showCts = null;
        }

        private void PrepareData(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            if (data.Result == GameResult.Victory)
            {
                _title.text = _localization.Get(Result.Victory);
            }
            else
            {
                _title.text = _localization.Get(Result.Defeat);
            }

            _goldValue.text = $"+{data.GoldEarned}";
            _unitsList.Clear();

            if (data.ClaimedMilestones is { Length: > 0 })
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
            int killsRequired = 0;
            int bonusSlots = 0;
            bool hasUnits = false;

            foreach (var milestone in milestones)
            {
                killsRequired = Mathf.Max(killsRequired, milestone.KillsRequired);
                bonusSlots += milestone.BonusArmySlots;

                if (milestone.Rewards == null)
                {
                    continue;
                }

                foreach (var entry in milestone.Rewards)
                {
                    AddRewardUnit(entry.CharacterData, entry.Count);
                    hasUnits = true;
                }
            }

            _taskLabel.text = _localization.Get(Result.TaskComplete, killsRequired);
            _taskLabel.SetDisplayed(true);

            _slotsLabel.text = _localization.Get(Result.BonusSlots, bonusSlots);
            _slotsLabel.SetDisplayed(bonusSlots > 0);

            _unitsLabel.text = _localization.Get(Result.NewUnits);
            _unitsLabel.SetDisplayed(hasUnits);
        }

        private void PrepareProgress(int currentKills, int nextMilestoneKills)
        {
            _taskLabel.SetDisplayed(false);
            _slotsLabel.SetDisplayed(false);
            _unitsLabel.SetDisplayed(true);

            if (nextMilestoneKills > 0)
            {
                _unitsLabel.text = _localization.Get(Result.Progress, currentKills, nextMilestoneKills);
            }
            else
            {
                _unitsLabel.text = _localization.Get(Result.AllRewards);
            }
        }

        private void AddRewardUnit(CharacterData characterData, int count)
        {
            var unit = _unitsList.AddChild(new VisualElement(), PortraitClass);
            unit.AddToClassList(RewardUnitClass);
            unit.SetImage(_unitPreviewService.GetPortrait(characterData, 0));

            if (count > 1)
            {
                unit.AddChild(new Label($"x{count}"), RewardUnitCountClass);
            }
        }

        private async UniTaskVoid ShowSequenceAsync(CancellationToken ct)
        {
            int delayMs = (int)(_elementDelay * MillisecondsPerSecond);

            _title.SetVisible(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _gold.SetVisible(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _units.SetVisible(true);
            await UniTask.Delay(delayMs, cancellationToken: ct);

            _doubleRewardButton.SetVisible(true);
            await UniTask.Delay((int)(_noThanksDelay * MillisecondsPerSecond), cancellationToken: ct);

            _noThanksButton.SetVisible(true);
            _restartButton.SetVisible(true);
        }

        private void HideAllElements()
        {
            _title.SetVisible(false);
            _gold.SetVisible(false);
            _units.SetVisible(false);
            _doubleRewardButton.SetVisible(false);
            _noThanksButton.SetVisible(false);
            _restartButton.SetVisible(false);
            SetButtonsInteractable(true);
        }

        private void PlayLoops()
        {
            _sunTween?.Kill();
            _sunAngle = 0f;
            _sunTween = DOTween.To(GetSunAngle, SetSunAngle, SunTurnDegrees, SunTurnDuration)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);

            _pulseTween?.Kill();
            _pulse = 1f;
            _pulseTween = DOTween.To(GetPulse, SetPulse, PulseScale, PulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private float GetSunAngle()
        {
            return _sunAngle;
        }

        private void SetSunAngle(float angle)
        {
            _sunAngle = angle;
            _sun.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
        }

        private float GetPulse()
        {
            return _pulse;
        }

        private void SetPulse(float scale)
        {
            _pulse = scale;
            _doubleRewardButton.style.scale = new Scale(new Vector2(scale, scale));
        }

        private void OnDoubleRewardClicked()
        {
            _audioService.PlayUIClick();
            SetButtonsInteractable(false);
            _adService.ShowRewarded(RewardedTag, OnDoubleRewardAdClosed);
        }

        private void OnDoubleRewardAdClosed(bool success)
        {
            if (success)
            {
                _playerProgressService.AddGold(_goldEarned);
                _playerProgressService.Save();
            }

            _sceneService.LoadBootScene();
        }

        private void OnNoThanksClicked()
        {
            _audioService.PlayUIClick();
            SetButtonsInteractable(false);
            _adService.ShowInterstitial(LoadBootScene);
        }

        private void OnRestartClicked()
        {
            _audioService.PlayUIClick();
            SetButtonsInteractable(false);
            _adService.ShowInterstitial(RestartLevel);
        }

        private void LoadBootScene()
        {
            _sceneService.LoadBootScene();
        }

        private void RestartLevel()
        {
            _sceneService.RestartLevel();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _doubleRewardButton.SetEnabled(interactable);
            _noThanksButton.SetEnabled(interactable);
            _restartButton.SetEnabled(interactable);
        }
    }
}
