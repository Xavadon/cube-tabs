using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using _Project.Scripts.Gameplay.UI.Kit;
using _Project.Scripts.Gameplay.UI.Menu;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static _Project.Scripts.Architecture.Services.Localization.LocalizationKeys;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    [RequireComponent(typeof(UIDocument))]
    public class LevelMapView : MonoBehaviour, IMenuScreen
    {
        private const int FirstMilestoneIndex = 0;

        private const string RootName = "level-map";
        private const string CloseButtonName = "close-button";
        private const string MapFrameName = "map-frame";
        private const string MapName = "map";
        private const string PathName = "level-path";
        private const string InfoName = "info";
        private const string LevelNameName = "level-name";
        private const string DescriptionName = "level-description";
        private const string StatsName = "level-stats";
        private const string PlayButtonName = "play-button";

        private const string PointClass = "level-point";
        private const string SelectedPointClass = "btn--gold";
        private const string LineClass = "level-info__line";
        private const string KillsLineClass = "level-info__line--kills";
        private const string DoneLineClass = "level-info__line--done";
        private const string ReadyLineClass = "level-info__line--ready";
        private const string NextLineClass = "level-info__line--next";
        private const string LockedLineClass = "level-info__line--locked";

        [SerializeField]
        private UIDocument _document;

        private readonly List<Button> _points = new();

        private IGameSessionService _sessionService;
        private ISceneService _sceneService;
        private IPlayerProgressService _progress;
        private IAudioService _audioService;
        private ILocalizationService _localization;
        private LevelConfig[] _levels;
        private LevelConfig _selectedLevel;

        private VisualElement _root;
        private VisualElement _mapFrame;
        private VisualElement _map;
        private VisualElement _info;
        private Label _levelName;
        private Label _levelDescription;
        private VisualElement _stats;
        private Button _playButton;
        private Button _closeButton;

        public void Initialize(LevelCatalog catalog, IGameSessionService sessionService, ISceneService sceneService,
            IPlayerProgressService progress, IAudioService audioService, ILocalizationService localization)
        {
            _sessionService = sessionService;
            _sceneService = sceneService;
            _progress = progress;
            _audioService = audioService;
            _localization = localization;
            _levels = catalog.Levels;

            _root = _document.rootVisualElement.Q(RootName);
            UILocalization.Apply(_root, localization);

            _mapFrame = _root.Q(MapFrameName);
            _map = _root.Q(MapName);
            _info = _root.Q(InfoName);
            _levelName = _root.Q<Label>(LevelNameName);
            _levelDescription = _root.Q<Label>(DescriptionName);
            _stats = _root.Q(StatsName);
            _playButton = _root.Q<Button>(PlayButtonName);
            _closeButton = _root.Q<Button>(CloseButtonName);

            _root.Query<Button>(className: PointClass).ToList(_points);

            for (int i = 0; i < _points.Count; i++)
            {
                var point = _points[i];

                if (i < _levels.Length)
                {
                    point.SetEnabled(IsLevelUnlocked(_levels, i, progress));
                    point.RegisterCallback<ClickEvent>(OnPointClicked);
                }
                else
                {
                    point.SetDisplayed(false);
                }
            }

            _root.Q<LevelPath>(PathName).SetPoints(_points);

            _mapFrame.RegisterCallback<GeometryChangedEvent>(OnMapFrameResized);
            _playButton.clicked += StartLevel;
            _closeButton.clicked += OnCloseClicked;

            _info.SetVisible(false);
            _localization.OnLanguageChanged += UpdateLocalizedTexts;
            Hide();
        }

        public void Show()
        {
            _root.SetDisplayed(true);

            if (_selectedLevel != null)
            {
                RefreshStats();
            }
        }

        public void Hide()
        {
            _root.SetDisplayed(false);
        }

        private void OnDestroy()
        {
            if (_localization != null)
            {
                _localization.OnLanguageChanged -= UpdateLocalizedTexts;
            }

            if (_root == null)
            {
                return;
            }

            for (int i = 0; i < _points.Count; i++)
            {
                _points[i].UnregisterCallback<ClickEvent>(OnPointClicked);
            }

            _mapFrame.UnregisterCallback<GeometryChangedEvent>(OnMapFrameResized);
            _playButton.clicked -= StartLevel;
            _closeButton.clicked -= OnCloseClicked;
        }

        private static bool IsLevelUnlocked(LevelConfig[] levels, int position, IPlayerProgressService progress)
        {
            if (position == 0)
            {
                return true;
            }

            if (progress.GetLevelKills(levels[position].LevelIndex) > 0)
            {
                return true;
            }

            LevelConfig previous = levels[position - 1];
            if (previous.Milestones == null || previous.Milestones.Length == 0)
            {
                return progress.IsLevelCompleted(previous.LevelIndex);
            }

            return progress.IsMilestoneClaimed(previous.LevelIndex, FirstMilestoneIndex);
        }

        private void OnMapFrameResized(GeometryChangedEvent evt)
        {
            Sprite mapSprite = _map.resolvedStyle.backgroundImage.sprite;
            if (mapSprite == null)
            {
                return;
            }

            float aspect = mapSprite.rect.width / mapSprite.rect.height;
            Rect area = _mapFrame.contentRect;
            float width = area.width;
            float height = width / aspect;

            if (height > area.height)
            {
                height = area.height;
                width = height * aspect;
            }

            _map.style.width = width;
            _map.style.height = height;
        }

        private void OnPointClicked(ClickEvent evt)
        {
            int index = _points.IndexOf((Button)evt.currentTarget);
            _audioService.PlayUIClick();
            SelectLevel(index);
        }

        private void OnCloseClicked()
        {
            _audioService.PlayUIClick();
            Hide();
        }

        private void SelectLevel(int index)
        {
            for (int i = 0; i < _points.Count; i++)
            {
                _points[i].EnableInClassList(SelectedPointClass, i == index);
            }

            _selectedLevel = _levels[index];
            _levelName.text = _localization.GetName(_selectedLevel);
            _info.SetVisible(true);

            RefreshDescription();
            RefreshStats();
        }

        private void StartLevel()
        {
            if (_selectedLevel == null)
            {
                Debug.LogError("[LevelMapView] Selected level is null");
                return;
            }

            if (_progress.ArmyCount == 0)
            {
                return;
            }

            _audioService.PlayUIClick();
            _sessionService.SelectLevel(_selectedLevel);
            _sceneService.LoadGameScene().Forget();
        }

        private void RefreshDescription()
        {
            if (string.IsNullOrEmpty(_selectedLevel.DescriptionKey))
            {
                _levelDescription.text = string.Empty;
                return;
            }

            _levelDescription.text = _localization.Get(_selectedLevel.DescriptionKey);
        }

        private void UpdateLocalizedTexts()
        {
            UILocalization.Apply(_root, _localization);

            if (_selectedLevel == null)
            {
                return;
            }

            _levelName.text = _localization.GetName(_selectedLevel);
            RefreshDescription();
            RefreshStats();
        }

        private void RefreshStats()
        {
            int levelIndex = _selectedLevel.LevelIndex;
            int currentKills = _progress.GetLevelKills(levelIndex);

            _stats.Clear();

            var milestones = _selectedLevel.Milestones;
            if (milestones is { Length: > 0 })
            {
                AddStatLine(_localization.Get(Level.Kills, currentKills), KillsLineClass);

                bool nextShown = false;
                for (int i = 0; i < milestones.Length; i++)
                {
                    int required = milestones[i].KillsRequired;

                    if (_progress.IsMilestoneClaimed(levelIndex, i))
                    {
                        AddStatLine(_localization.Get(Level.MilestoneDone, required), DoneLineClass);
                    }
                    else if (currentKills >= required)
                    {
                        AddStatLine(_localization.Get(Level.MilestoneReady, required), ReadyLineClass);
                    }
                    else if (!nextShown)
                    {
                        nextShown = true;
                        AddStatLine(_localization.Get(Level.MilestoneNext, required, required - currentKills), NextLineClass);
                    }
                    else
                    {
                        AddStatLine(_localization.Get(Level.MilestoneLocked, required), LockedLineClass);
                    }
                }
            }

            bool armyReady = _progress.ArmyCount > 0;
            _playButton.SetEnabled(armyReady);

            if (armyReady)
            {
                _playButton.text = _localization.Get(Level.Play);
            }
            else
            {
                _playButton.text = _localization.Get(Level.ArmyEmpty);
            }
        }

        private void AddStatLine(string text, string modifierClass)
        {
            var line = _stats.AddChild(new Label(text), LineClass);
            line.AddToClassList(modifierClass);
        }
    }
}
