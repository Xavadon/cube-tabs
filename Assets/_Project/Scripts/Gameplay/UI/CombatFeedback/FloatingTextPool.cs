using _Project.Scripts.Architecture;
using _Project.Scripts.Gameplay.Services.CombatFeedback;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.CombatFeedback
{
    public class FloatingTextPool : MonoBehaviour
    {
        [SerializeField]
        private FloatingTextElement _prefab;

        [SerializeField]
        private int _preloadCount = 30;

        [SerializeField]
        private int _maxActive = 60;

        private Pool<FloatingTextElement> _pool;
        private Camera _camera;
        private int _activeCount;

        private void Awake()
        {
            _camera = Camera.main;
            _pool = new Pool<FloatingTextElement>(CreateElement, null, DeactivateElement, _preloadCount);
        }

        public void TryShow(Vector3 worldPosition, float value, FloatingTextStyle style, float scale, float horizontalOffset)
        {
            if (_activeCount >= _maxActive || _camera == null)
            {
                return;
            }

            FloatingTextElement element = _pool.Get();
            _activeCount++;
            element.Play(worldPosition, value, style, scale, horizontalOffset, _camera);
        }

        public void Return(FloatingTextElement element)
        {
            _activeCount--;
            _pool.Return(element);
        }

        private FloatingTextElement CreateElement()
        {
            FloatingTextElement element = Instantiate(_prefab, transform);
            element.Setup(this);
            element.gameObject.SetActive(false);
            return element;
        }

        private static void DeactivateElement(FloatingTextElement element)
        {
            element.gameObject.SetActive(false);
        }
    }
}
