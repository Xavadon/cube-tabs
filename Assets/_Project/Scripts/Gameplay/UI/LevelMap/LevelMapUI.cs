using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    public class LevelMapUI : MonoBehaviour
    {
        [SerializeField]
        private LevelPointUI[] _levelPoints;

        [SerializeField]
        private LevelInfoUI _levelInfo;

        [SerializeField]
        private Button _closeButton;

        private IAudioService _audioService;

        public void Initalize(LevelCatalog catalog, IGameSessionService sessionService, ISceneService sceneService,
            IPlayerProgressService progress, IAudioService audioService)
        {
            _audioService = audioService;

            var levels = catalog.Levels;

            for (int i = 0; i < _levelPoints.Length; i++)
            {
                if (i < levels.Length)
                {
                    var level = levels[i];
                    _levelPoints[i].Initialize(() =>
                    {
                        audioService?.PlayUIClick();
                        OnLevelSelected(level);
                    });
                    _levelPoints[i].gameObject.SetActive(true);
                }
                else
                {
                    _levelPoints[i].gameObject.SetActive(false);
                }
            }

            _levelInfo.Initalize(sessionService, sceneService, progress, audioService);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            gameObject.SetActive(false);
        }

        private void OnLevelSelected(LevelConfig level)
        {
            _levelInfo.SelectLevel(level);
        }

        private void OnCloseButtonClicked()
        {
            _audioService?.PlayUIClick();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
    }
}
