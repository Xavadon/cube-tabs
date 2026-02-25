using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI
{
    public class LevelMapUI : MonoBehaviour
    {
        private const string CatalogPath = "Data/LevelCatalog";

        [SerializeField]
        private LevelPointUI _levelPointPrefab;

        [SerializeField]
        private Transform _pointsContainer;

        private void Start()
        {
            var catalog = Resources.Load<LevelCatalog>(CatalogPath);

            if (catalog == null)
            {
                Debug.LogError($"[LevelMapUI] LevelCatalog not found at '{CatalogPath}'");
                return;
            }

            foreach (var level in catalog.Levels)
            {
                LevelPointUI point = Instantiate(_levelPointPrefab, _pointsContainer);
                point.Init(level.LevelName, () => OnLevelSelected(level));
            }
        }

        private void OnLevelSelected(LevelConfig level)
        {
            var sessionService = Project.Get<IGameSessionService>();
            sessionService.SelectLevel(level);

            var sceneService = Project.Get<SceneService>();
            sceneService.LoadGameScene().Forget();
        }
    }
}
