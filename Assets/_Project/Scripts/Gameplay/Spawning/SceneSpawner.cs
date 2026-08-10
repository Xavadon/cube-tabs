using UnityEngine;

namespace _Project.Scripts.Gameplay.Spawning
{
    // спавнеры не лезут в сервисы сами: мир поднимает ArpgSceneRoot и дёргает Begin, когда всё готово.
    // этапы данжа снимают галку и запускаются, только когда игрок до них дошёл
    public abstract class SceneSpawner : MonoBehaviour
    {
        [SerializeField] private bool _autoBegin = true;

        public bool AutoBegin => _autoBegin;

        public abstract void Begin();
    }
}
