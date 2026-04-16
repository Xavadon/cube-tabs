using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.LevelMap
{
    public class LevelMapUI : MonoBehaviour
    {
        [SerializeField]
        private LevelPointUI[] _levelPoints;

        [SerializeField]
        private LevelInfoUI _levelInfo;

        public void Initalize(LevelCatalog catalog, IGameSessionService sessionService, ISceneService sceneService,
            IPlayerProgressService progress)
        {
            var levels = catalog.Levels;

            for (int i = 0; i < _levelPoints.Length; i++)
            {
                if (i < levels.Length)
                {
                    var level = levels[i];
                    _levelPoints[i].Initialize(() => OnLevelSelected(level));
                    _levelPoints[i].gameObject.SetActive(true);
                }
                else
                {
                    _levelPoints[i].gameObject.SetActive(false);
                }
            }

            _levelInfo.Initalize(sessionService, sceneService, progress);

            gameObject.SetActive(false);
        }

        private void OnLevelSelected(LevelConfig level)
        {
            _levelInfo.SelectLevel(level);
        }
    }
}
