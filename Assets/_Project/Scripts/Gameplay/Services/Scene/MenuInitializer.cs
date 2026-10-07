using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Audio;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.UI.Army;
using _Project.Scripts.Gameplay.UI.LevelMap;
using _Project.Scripts.Gameplay.UI.Menu;
using _Project.Scripts.Gameplay.UI.Settings;
using _Project.Scripts.Gameplay.UI.Shop;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class MenuInitializer : IMenuInitializer
    {
        private const string MenuPrefabPath = "Prefab/UI/Menu";
        private const string SettingsPrefabPath = "Prefab/UI/Settings";
        private const string LevelCatalogPath = "Data/LevelCatalog";
        private const string ShopCatalogPath = "Data/ShopCatalog";

        private readonly IGameSessionService _gameSessionService;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IPurchaseService _purchaseService;
        private readonly IUnitPreviewService _unitPreviewService;
        private readonly IAdService _adService;
        private readonly IAudioService _audioService;
        private readonly ILocalizationService _localizationService;

        public MenuInitializer(
            IGameSessionService gameSessionService,
            IPlayerProgressService playerProgressService,
            IPurchaseService purchaseService,
            IUnitPreviewService unitPreviewService,
            IAdService adService,
            IAudioService audioService,
            ILocalizationService localizationService)
        {
            _gameSessionService = gameSessionService;
            _playerProgressService = playerProgressService;
            _purchaseService = purchaseService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
            _audioService = audioService;
            _localizationService = localizationService;
        }

        public UniTask Initialize()
        {
            Debug.Log("[MenuInitializer] Initialized");
            return UniTask.CompletedTask;
        }

        public void InitializeMenu(ISceneService sceneService)
        {
            var menuPrefab = Resources.Load<MenuView>(MenuPrefabPath);

            if (menuPrefab == null)
            {
                Debug.LogError($"[MenuInitializer] Menu prefab not found at '{MenuPrefabPath}'");
                return;
            }

            var menu = Object.Instantiate(menuPrefab);
            var levelMap = menu.GetComponent<LevelMapView>();
            var army = menu.GetComponent<ArmyScreenView>();
            var shop = menu.GetComponent<ShopScreenView>();

            var levelCatalog = Resources.Load<LevelCatalog>(LevelCatalogPath);
            var shopCatalog = Resources.Load<ShopCatalog>(ShopCatalogPath);

            levelMap.Initialize(levelCatalog, _gameSessionService, sceneService, _playerProgressService, _audioService, _localizationService);
            shop.Initialize(_playerProgressService, _purchaseService, _unitPreviewService, shopCatalog, _audioService, _localizationService);
            army.Initialize(_playerProgressService, shopCatalog, _unitPreviewService, _adService, _audioService, _localizationService);
            menu.Initialize(_audioService, _localizationService, levelMap, army, shop);

            InitializeSettings();

            _audioService.PlayMusic(MusicTheme.Menu);

            Debug.Log("[MenuInitializer] Menu initialized");
        }

        private void InitializeSettings()
        {
            var settingsPrefab = Resources.Load<SettingsView>(SettingsPrefabPath);

            if (settingsPrefab == null)
            {
                Debug.LogError($"[MenuInitializer] Settings prefab not found at '{SettingsPrefabPath}'");
                return;
            }

            Object.Instantiate(settingsPrefab).Initialize(_audioService, _localizationService);
        }
    }
}
