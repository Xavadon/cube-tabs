using System.Text;
using _Project.Scripts.Architecture.Services.Audio;
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
        private TMP_Text _levelStats;

        [SerializeField]
        private Button _playButton;

        private readonly StringBuilder _statsBuilder = new();

        private IGameSessionService _sessionService;
        private ISceneService _sceneService;
        private IPlayerProgressService _progress;
        private IAudioService _audioService;
        private LevelConfig _selectedLevel;
        
        private void OnEnable()
        {
            _playButton.onClick.AddListener(StartLevel);
        }

        private void OnDisable()
        {
            _playButton.onClick.RemoveListener(StartLevel);
        }

        public void Initalize(IGameSessionService sessionService, ISceneService sceneService,
            IPlayerProgressService progress, IAudioService audioService)
        {
            _sessionService = sessionService;
            _sceneService = sceneService;
            _progress = progress;
            _audioService = audioService;

            gameObject.SetActive(false);
        }

        private void StartLevel()
        {
            if (_selectedLevel == null)
            {
                Debug.LogError("[LevelInfoUI] Selected level is null");
                return;
            }

            _audioService?.PlayUIClick();
            _sessionService.SelectLevel(_selectedLevel);
            _sceneService.LoadGameScene().Forget();
        }


        public void SelectLevel(LevelConfig level)
        {
            gameObject.SetActive(true);
            _selectedLevel = level;
            _levelName.text = _selectedLevel.LevelName;
            RefreshStats();
        }

        private void RefreshStats()
        {
            int levelIndex = _selectedLevel.LevelIndex;
            int currentKills = _progress.GetLevelKills(levelIndex);
            bool completed = _progress.IsLevelCompleted(levelIndex);

            _statsBuilder.Clear();

            var milestones = _selectedLevel.Milestones;
            if (milestones is { Length: > 0 })
            {
                _statsBuilder.Append("Kills: ").AppendLine(currentKills.ToString());

                for (int i = 0; i < milestones.Length; i++)
                {
                    bool claimed = _progress.IsMilestoneClaimed(levelIndex, i);
                    string status = claimed ? "✓" : currentKills >= milestones[i].KillsRequired ? "!" : " ";
                    _statsBuilder.Append("[").Append(status).Append("] ")
                        .Append(milestones[i].KillsRequired).AppendLine(" kills");
                }
            }

            _levelStats.text = _statsBuilder.ToString();
            _playButton.interactable = !completed;
        }
    }
}