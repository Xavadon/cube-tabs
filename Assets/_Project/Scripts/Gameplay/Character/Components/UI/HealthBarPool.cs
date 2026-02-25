using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.UI
{
    public class HealthBarPool : MonoBehaviour
    {
        //TODO: добавить базовый класс пул
        
        [SerializeField]
        private HealthBarElement _prefab;

        [SerializeField]
        private int _preloadCount = 20;

        private readonly Stack<HealthBarElement> _pool = new();
        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;

            for (int i = 0; i < _preloadCount; i++)
            {
                HealthBarElement element = CreateElement();
                element.gameObject.SetActive(false);
                _pool.Push(element);
            }
        }

        public HealthBarElement Get()
        {
            HealthBarElement element = _pool.Count > 0 ? _pool.Pop() : CreateElement();
            return element;
        }

        public void Return(HealthBarElement element)
        {
            element.gameObject.SetActive(false);
            _pool.Push(element);
        }

        public Camera GetCamera() => _camera;

        private HealthBarElement CreateElement()
        {
            return Instantiate(_prefab, transform);
        }
    }
}
