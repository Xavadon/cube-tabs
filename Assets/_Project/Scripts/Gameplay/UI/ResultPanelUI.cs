using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
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
        private Button _menuButton;

        private ISceneService _sceneService;

        private void OnEnable()
        {
            _menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnDisable()
        {
            _menuButton.onClick.RemoveListener(OnMenuClicked);
        }

        public void Initialize(ISceneService sceneService)
        {
            _sceneService = sceneService;
        }

        public void Show(GameResultData data, int currentLevelKills, int killsToComplete)
        {
            if (data.Result == GameResult.Victory)
            {
                _titleText.text = "Победа";
            }
            else
            {
                _titleText.text = "Поражение";
            }

            string progress;
            if (killsToComplete > 0)
            {
                progress = $"\n\n{currentLevelKills}/{killsToComplete} убийств до завершения";
            }
            else
            {
                progress = string.Empty;
            }

            _statsText.text = $"+{data.GoldEarned} золота\n\n{data.EnemiesKilled} врагов убито{progress}";

            gameObject.SetActive(true);
        }

        private void OnMenuClicked()
        {
            _sceneService.LoadBootScene();
        }
    }
}
