using System;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    public class LevelInfoUI : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _levelName;

        [SerializeField]
        private TMP_Text _levelDescription;

        [SerializeField]
        private Button _playButton;

        private IGameSessionService _sessionService;
        private ISceneService _sceneService;
        private LevelConfig _selectedLevel;
        
        private void OnEnable()
        {
            _playButton.onClick.AddListener(StartLevel);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(StartLevel);
        }

        public void Initalize(IGameSessionService sessionService, ISceneService sceneService)
        {
            _sessionService = sessionService;
            _sceneService = sceneService;

            gameObject.SetActive(false);
        }

        private void StartLevel()
        {
            if (_selectedLevel == null)
            {
                Debug.LogError("[LevelInfoUI] Selected level is null");
                return;
            }
            
            _sessionService.SelectLevel(_selectedLevel);
            _sceneService.LoadGameScene().Forget();
        }

        public void SelectLevel(LevelConfig level)
        {
            gameObject.SetActive(true);
            _selectedLevel = level;
            _levelName.text = _selectedLevel.LevelName;
        }
    }
}