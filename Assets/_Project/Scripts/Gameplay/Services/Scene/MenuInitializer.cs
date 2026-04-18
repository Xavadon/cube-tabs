using _Project.Scripts.Architecture.Services;
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

        private readonly IGameSessionService _gameSessionService;
        private readonly IPlayerProgressService _playerProgressService;
        private readonly IPurchaseService _purchaseService;
        private readonly IUnitPreviewService _unitPreviewService;
        private readonly IAdService _adService;

        public MenuInitializer(
            IGameSessionService gameSessionService,
            IPlayerProgressService playerProgressService,
            IPurchaseService purchaseService,
            IUnitPreviewService unitPreviewService,
            IAdService adService)
        {
            _gameSessionService = gameSessionService;
            _playerProgressService = playerProgressService;
            _purchaseService = purchaseService;
            _unitPreviewService = unitPreviewService;
            _adService = adService;
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
            var shop = canvas.GetComponentInChildren<ShopScreenView>();
            var army = canvas.GetComponentInChildren<ArmyScreenView>();

            var levelCatalog = Resources.Load<LevelCatalog>(LevelCatalogPath);
            var shopCatalog = Resources.Load<ShopCatalog>(ShopCatalogPath);

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
                shop.Initialize(_playerProgressService, _purchaseService, _unitPreviewService, shopCatalog);
            }
            else
            {
                Debug.LogError("[MenuInitializer] ShopScreenView not found on MenuCanvas prefab");
            }

            if (army != null)
            {
                army.Initialize(_playerProgressService, shopCatalog, _unitPreviewService, _adService);
            }
            else
            {
                Debug.LogError("[MenuInitializer] ArmyScreenView not found on MenuCanvas prefab");
            }

            Debug.Log("[MenuInitializer] Menu initialized");
        }
    }
}
