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
        private TextMeshProUGUI _goldText;

        [SerializeField]
        private TextMeshProUGUI _killsText;

        [SerializeField]
        private TextMeshProUGUI _progressText;

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
            _titleText.text = data.Result == GameResult.Victory ? "Победа" : "Поражение";
            _goldText.text = $"+{data.GoldEarned} золота";
            _killsText.text = $"{data.EnemiesKilled} врагов убито";

            if (killsToComplete > 0)
                _progressText.text = $"{currentLevelKills}/{killsToComplete} убийств до завершения";
            else
                _progressText.text = string.Empty;

            gameObject.SetActive(true);
        }

        private void OnMenuClicked()
        {
            _sceneService.LoadBootScene();
        }
    }
}
