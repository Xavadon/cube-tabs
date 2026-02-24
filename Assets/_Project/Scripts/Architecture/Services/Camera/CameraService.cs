using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public class CameraService : ICameraService
    {
        private const float SmoothTime = 0.5f;
        private const float MinDistance = 12f;
        private const float MaxDistance = 30f;
        private const float BoundsPadding = 3f;

        private static readonly Vector3 BaseOffset = new(0f, 15f, -10f);

        public bool CameraLocked { get; private set; }

        private readonly ICharacterRegistry _characterRegistry;
        private IReadOnlyList<Character> _allies;
        private UnityEngine.Camera _playerCamera;
        private Vector3 _smoothCenter;
        private Vector3 _centerVelocity;

        public CameraService(ICharacterRegistry characterRegistry)
        {
            _characterRegistry = characterRegistry;
        }

        public UniTask Initialize()
        {
            _playerCamera = UnityEngine.Camera.main;
            _allies = _characterRegistry.GetAllies();
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            if (_playerCamera == null)
            {
                _playerCamera = UnityEngine.Camera.main;
                if (_playerCamera == null) return;
            }

            if (_allies == null || _allies.Count == 0) return;

            Vector3 rawCenter = Vector3.zero;
            Bounds bounds = new Bounds(_allies[0].transform.position, Vector3.zero);

            for (int i = 0; i < _allies.Count; i++)
            {
                Vector3 pos = _allies[i].transform.position;
                rawCenter += pos;
                bounds.Encapsulate(pos);
            }

            rawCenter /= _allies.Count;

            _smoothCenter = Vector3.SmoothDamp(_smoothCenter, rawCenter, ref _centerVelocity, SmoothTime);

            float spread = Mathf.Max(bounds.size.x, bounds.size.z) + BoundsPadding;
            float distanceFactor = Mathf.Clamp(spread / 10f, 1f, MaxDistance / MinDistance);
            Vector3 offset = BaseOffset * distanceFactor;

            Transform camTransform = _playerCamera.transform;
            camTransform.position = _smoothCenter + offset;
            camTransform.LookAt(_smoothCenter);
        }
    }
}
