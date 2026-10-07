using _Project.Scripts.Architecture;
using _Project.Scripts.Gameplay.Character.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.UI
{
    public class HealthBarPool : MonoBehaviour
    {
        [SerializeField]
        private HealthBarElement _prefab;

        [SerializeField]
        private int _preloadCount = 20;

        [Header("Team Colors")]
        [SerializeField]
        private Color _allyColor = new(0.3f, 0.85f, 0.35f);

        [SerializeField]
        private Color _enemyColor = new(0.95f, 0.25f, 0.2f);

        private Pool<HealthBarElement> _pool;
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;

            _pool = new Pool<HealthBarElement>(
                createFunc: () =>
                {
                    HealthBarElement element = Instantiate(_prefab, transform);
                    element.gameObject.SetActive(false);
                    return element;
                },
                onGet: null,
                onReturn: element => element.gameObject.SetActive(false),
                preloadCount: _preloadCount);
        }

        public HealthBarElement Get() => _pool.Get();

        public void Return(HealthBarElement element) => _pool.Return(element);

        public Camera GetCamera() => _camera;

        public Color GetTeamColor(CharacterType characterType)
        {
            if (characterType == CharacterType.Ally)
            {
                return _allyColor;
            }

            return _enemyColor;
        }
    }
}
