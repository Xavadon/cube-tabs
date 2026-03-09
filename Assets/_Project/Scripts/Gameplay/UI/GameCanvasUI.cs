using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI
{
    public class GameCanvasUI : MonoBehaviour
    {
        [SerializeField]
        private ResultPanelUI _resultPanel;

        private IPlayerProgressService _playerProgressService;
        private LevelConfig _levelConfig;

        public void Initialize(ISceneService sceneService, IPlayerProgressService playerProgressService, LevelConfig levelConfig)
        {
            _playerProgressService = playerProgressService;
            _levelConfig = levelConfig;

            _resultPanel.Initialize(sceneService);
            _resultPanel.gameObject.SetActive(false);
        }

        public void ShowResult(GameResultData data)
        {
            int currentKills = _playerProgressService.GetLevelKills(_levelConfig.LevelIndex);
            int killsToComplete = _levelConfig.KillsToComplete;

            _resultPanel.Show(data, currentKills, killsToComplete);
        }
    }
}
