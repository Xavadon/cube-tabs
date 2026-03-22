using System;
using System.Text;
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
            IPlayerProgressService progress)
        {
            _sessionService = sessionService;
            _sceneService = sceneService;
            _progress = progress;

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
            RefreshStats();
        }

        private void RefreshStats()
        {
            int levelIndex = _selectedLevel.LevelIndex;
            int currentKills = _progress.GetLevelKills(levelIndex);
            int requiredKills = _selectedLevel.KillsToComplete;
            bool completed = _progress.IsLevelCompleted(levelIndex);

            _statsBuilder.Clear();
            _statsBuilder.Append("Kills: ").Append(currentKills).Append("/").AppendLine(requiredKills.ToString());

            if (_selectedLevel.Waves is { Length: > 0 })
            {
                for (int w = 0; w < _selectedLevel.Waves.Length; w++)
                {
                    var wave = _selectedLevel.Waves[w];

                    if (wave.Entries == null || wave.Entries.Length == 0)
                        continue;

                    _statsBuilder.Append("Wave ").Append(w + 1).AppendLine(":");

                    foreach (var entry in wave.Entries)
                    {
                        string name = entry.CharacterData.MaxTier > 0
                            ? $"{entry.CharacterData.Name} T{entry.TierIndex + 1}"
                            : entry.CharacterData.Name;

                        _statsBuilder.Append("  ").Append(name).Append(" x").AppendLine(entry.Count.ToString());
                    }
                }
            }

            _levelStats.text = _statsBuilder.ToString();
            _playButton.interactable = !completed;
        }
    }
}