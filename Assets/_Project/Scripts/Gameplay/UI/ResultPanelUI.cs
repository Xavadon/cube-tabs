using System.Text;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class ResultPanelUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _titleText;

        [SerializeField]
        private TextMeshProUGUI _statsText;

        [SerializeField]
        private TextMeshProUGUI _rewardText;

        [SerializeField]
        private Button _menuButton;
        
        [SerializeField]
        private Image _rewardSun;
        
        private float _sunRotationDuration = 10f;
        private ISceneService _sceneService;
        private Tween _sunRotationTween;

        private void OnEnable()
        {
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDisable()
        {
            _menuButton.onClick.RemoveListener(OnMenuClicked);
            _sunRotationTween?.Kill();
            _sunRotationTween = null;
        }

        public void Initialize(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        public void Show(GameResultData data, int currentLevelKills, int nextMilestoneKills)
        {
            _sunRotationTween?.Kill();
            _rewardSun.transform.rotation = Quaternion.identity;
            _sunRotationTween = _rewardSun.transform
                .DORotate(new Vector3(0, 0, -360), _sunRotationDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
            
            if (data.Result == GameResult.Victory)
            {
                _titleText.text = "Победа";
            }
            else
            {
                _titleText.text = "Поражение";
            }

            string progress;
            if (nextMilestoneKills > 0)
            {
                progress = $"\n\n{currentLevelKills}/{nextMilestoneKills} убийств до награды";
            }
            else
            {
                progress = "\n\nВсе награды получены";
            }

            _statsText.text = $"+{data.GoldEarned} золота\n\n{data.EnemiesKilled} врагов убито{progress}";

            if (_rewardText != null)
            {
                bool hasMilestones = data.ClaimedMilestones is { Length: > 0 };

                if (hasMilestones)
                {
                    var sb = new StringBuilder();

                    foreach (var milestone in data.ClaimedMilestones)
                    {
                        sb.Append("Награда за ").Append(milestone.KillsRequired).AppendLine(" убийств:");

                        if (milestone.Rewards != null)
                        {
                            foreach (var entry in milestone.Rewards)
                            {
                                sb.Append("• ").Append(entry.CharacterData.Name);
                                if (entry.Count > 1)
                                    sb.Append(" x").Append(entry.Count);
                                sb.AppendLine();
                            }
                        }

                        if (milestone.BonusArmySlots > 0)
                            sb.Append("+").Append(milestone.BonusArmySlots).AppendLine(" слотов армии");
                    }

                    _rewardText.text = sb.ToString();
                    _rewardText.gameObject.SetActive(true);
                }
                else
                {
                    _rewardText.gameObject.SetActive(false);
                }
            }

            gameObject.SetActive(true);
        }

        private void OnMenuClicked()
        {
            _sceneService.LoadBootScene();
        }
    }
}
