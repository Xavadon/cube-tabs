using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Merchant;
using _Project.Scripts.Gameplay.Merchant.UI;
using _Project.Scripts.Gameplay.Spawning;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class ArpgSceneRoot : MonoBehaviour
    {
        [SerializeField] private Transform _playerSpawn;
        [SerializeField] private GameObject _uiPrefab;

        private ArpgConfig _config;
        private ICharacterRegistry _registry;
        private IEquipmentService _equipment;
        private HudView _hud;
        private CharacterEntity _player;
        private bool _ready;

        public CharacterEntity Player => _player;

        // сцену можно открыть напрямую в редакторе: тогда сервисы поднимает она сама,
        // из Boot же контейнер уже готов и второй раз не собирается
        private async void Start()
        {
            if (!Project.IsInitialized)
                await Project.InitializeServicesOnly();

            Build();
        }

        private void OnDestroy()
        {
            if (_equipment != null)
                _equipment.OnChanged -= HandleEquipmentChanged;
        }

        private void Build()
        {
            if (_ready)
                return;

            _ready = true;

            var startup = Project.Get<IArpgStartupService>();

            // Play со сцены минует Boot — тогда стартовый контент выдаём тут, сервис сам не даст задвоить
            startup.GrantStartingContent();
            _config = startup.Config;

            if (_config == null)
                return;

            _registry = Project.Get<ICharacterRegistry>();
            _equipment = Project.Get<IEquipmentService>();

            EnsureUI();

            _player = SpawnPlayer();
            _equipment.OnChanged += HandleEquipmentChanged;

            Project.Get<ICameraService>().SetTarget(_player.transform);

            BindUI();

            _registry.StartBattle();
            HandleEquipmentChanged();

            BeginSpawners();
        }

        private void BeginSpawners()
        {
            foreach (SceneSpawner spawner in FindObjectsByType<SceneSpawner>(FindObjectsSortMode.None))
            {
                if (spawner.AutoBegin)
                    spawner.Begin();
            }
        }

        // UI одна на все сцены: держим префабом, а не копией объектов в каждой
        private void EnsureUI()
        {
            if (_uiPrefab == null || FindAnyObjectByType<HudView>() != null)
                return;

            Instantiate(_uiPrefab);
        }

        private CharacterEntity SpawnPlayer()
        {
            var factory = Project.Get<ICharacterFactory>();

            CharacterEntity character = factory.Create(CharacterType.Ally, _config.PlayerUnit, _config.PlayerTier);
            character.SetRegistry(_registry);
            _registry.Register(character);

            SpawnPlacement.Place(character, ResolveSpawn());

            return character;
        }

        private Vector3 ResolveSpawn()
        {
            string entry = Project.Get<ISceneService>().PendingEntry;

            if (!string.IsNullOrEmpty(entry))
            {
                foreach (SceneEntryPoint point in FindObjectsByType<SceneEntryPoint>(FindObjectsSortMode.None))
                {
                    if (point.Id == entry)
                        return point.transform.position;
                }

                Debug.LogWarning($"[ArpgSceneRoot] Точка входа '{entry}' не найдена, ставлю игрока в дефолтную");
            }

            return _playerSpawn != null ? _playerSpawn.position : transform.position;
        }

        private void BindUI()
        {
            var localization = Project.Get<ILocalizationService>();
            var progress = Project.Get<IPlayerProgressService>();
            var merchant = FindAnyObjectByType<MerchantView>();

            if (merchant != null)
                merchant.Bind(Project.Get<IMerchantService>(), _equipment, progress, localization);

            _hud = FindAnyObjectByType<HudView>();

            if (_hud == null)
                return;

            _hud.Bind(progress, _equipment, localization, Project.Get<IUnitPreviewService>(), _player);

            if (merchant == null)
                return;

            _hud.OnMerchantClicked += merchant.Toggle;
            _hud.ItemClickInterceptor = merchant.TryShowRecipe;
        }

        private void HandleEquipmentChanged()
        {
            if (_player != null)
                _player.RefreshStats(_equipment.TotalBonus + _config.StartingStats);
        }
    }
}
