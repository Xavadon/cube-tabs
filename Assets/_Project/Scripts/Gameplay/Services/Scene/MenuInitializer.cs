using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.UI.Army;
using _Project.Scripts.Gameplay.UI.LevelMap;
using _Project.Scripts.Gameplay.UI.Shop;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    public class MenuInitializer : IMenuInitializer
    {
        private const string CanvasPrefabPath = "Prefab/MenuCanvas";
        private const string LevelCatalogPath = "Data/LevelCatalog";
        private const string ShopCatalogPath = "Data/ShopCatalog";
        private const string PreviewConfigPath = "Data/UnitPreviewConfig";
        private const string FullBodyPreviewConfigPath = "Data/UnitPreviewConfig_FullBody";

        private readonly IGameSessionService _gameSessionService;
        private readonly IPlayerProgressService _playerProgressService;

        public MenuInitializer(
            IGameSessionService gameSessionService,
            IPlayerProgressService playerProgressService)
        {
            _gameSessionService = gameSessionService;
            _playerProgressService = playerProgressService;
        }

        public UniTask Initialize()
        {
            Debug.Log("[MenuInitializer] Initialized");
            return UniTask.CompletedTask;
        }

        public void InitializeMenu(ISceneService sceneService)
        {
            var canvasPrefab = Resources.Load<GameObject>(CanvasPrefabPath);

            if (canvasPrefab == null)
            {
                Debug.LogError($"[MenuInitializer] MenuCanvas prefab not found at '{CanvasPrefabPath}'");
                return;
            }

            var canvas = Object.Instantiate(canvasPrefab);

            var levelMap = canvas.GetComponentInChildren<LevelMapUI>();
            var shop = canvas.GetComponentInChildren<ShopScreenUI>();
            var army = canvas.GetComponentInChildren<ArmyScreenView>();

            var levelCatalog = Resources.Load<LevelCatalog>(LevelCatalogPath);
            var shopCatalog = Resources.Load<ShopCatalog>(ShopCatalogPath);
            var previewConfig = Resources.Load<UnitPreviewConfig>(PreviewConfigPath);
            var fullBodyPreviewConfig = Resources.Load<UnitPreviewConfig>(FullBodyPreviewConfigPath);

            if (levelMap != null)
            {
                levelMap.Initalize(levelCatalog, _gameSessionService, sceneService, _playerProgressService);
            }
            else
            {
                Debug.LogError("[MenuInitializer] LevelMapUI not found on MenuCanvas prefab");
            }

            if (shop != null)
            {
                shop.Initialize(shopCatalog, _playerProgressService, previewConfig);
            }
            else
            {
                Debug.LogError("[MenuInitializer] ShopScreenUI not found on MenuCanvas prefab");
            }

            if (army != null)
            {
                army.Initialize(_playerProgressService, shopCatalog, previewConfig, fullBodyPreviewConfig);
            }
            else
            {
                Debug.LogError("[MenuInitializer] ArmyScreenUI not found on MenuCanvas prefab");
            }

            Debug.Log("[MenuInitializer] Menu initialized");
        }
    }
}
