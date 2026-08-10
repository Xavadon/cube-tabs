using UnityEngine;

namespace _Project.Scripts.Gameplay.Spawning
{
    // этап оживает, когда игрок в него вошёл: до этого крипы и босс не спавнятся
    public class DungeonStage : MonoBehaviour
    {
        [SerializeField] private SceneSpawner[] _spawners;

        private bool _begun;

        public void Begin()
        {
            if (_begun)
                return;

            _begun = true;

            foreach (SceneSpawner spawner in _spawners)
            {
                if (spawner != null)
                    spawner.Begin();
            }
        }
    }
}
