using System;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    public class LevelMapUI : MonoBehaviour
    {
        [SerializeField]
        private LevelPointUI _levelPointPrefab;

        [SerializeField]
        private LevelInfoUI _levelInfo;

        [SerializeField]
        private Transform _pointsContainer;
        
        public void Initalize(LevelCatalog catalog, IGameSessionService sessionService, ISceneService sceneService)
        {
            foreach (var level in catalog.Levels)
            {
                LevelPointUI point = Instantiate(_levelPointPrefab, _pointsContainer);
                point.Initialize(level.LevelName, () => OnLevelSelected(level));
            }

            _levelInfo.Initalize(sessionService, sceneService);
            
            gameObject.SetActive(false);
        }

        private void OnLevelSelected(LevelConfig level)
        {
            _levelInfo.SelectLevel(level);
        }
    }
}