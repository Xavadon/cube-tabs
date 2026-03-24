using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI
{
    public class GameCanvasUI : MonoBehaviour
    {
        [SerializeField]
        private ResultPanelUI _resultPanel;

        [SerializeField]
        private Button _cameraModeButton;

        private IPlayerProgressService _playerProgressService;
        private LevelConfig _levelConfig;

        public void Initialize(ISceneService sceneService, IPlayerProgressService playerProgressService,
            LevelConfig levelConfig, ICameraService cameraService)
        {
            _playerProgressService = playerProgressService;
            _levelConfig = levelConfig;

            _resultPanel.Initialize(sceneService);
            _resultPanel.gameObject.SetActive(false);

            if (_cameraModeButton != null)
                _cameraModeButton.onClick.AddListener(cameraService.CycleMode);
        }

        public void ShowResult(GameResultData data)
        {
            int currentKills = _playerProgressService.GetLevelKills(_levelConfig.LevelIndex);
            int nextMilestoneKills = GetNextMilestoneKills();

            _resultPanel.Show(data, currentKills, nextMilestoneKills);
        }

        private int GetNextMilestoneKills()
        {
            var milestones = _levelConfig.Milestones;
            if (milestones == null || milestones.Length == 0)
                return 0;

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
