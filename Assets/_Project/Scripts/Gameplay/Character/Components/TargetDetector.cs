using _Project.Scripts.Gameplay.Character.Components.Health;
using Game.Scripts.Core.Gameplay.Character.Health;
using UnityEngine;

namespace Game.Scripts.Core.Gameplay.Enemy
{
    public class TargetDetector
    {
        private readonly Transform _transform;
        private readonly float _detectionRadius;
        private readonly float _fieldOfView;

        private IDamageAble _currentTarget;
        private float _updateInterval = 0.5f;
        private float _timeSinceLastUpdate;

        public TargetDetector(Transform transform, float detectionRadius, float fieldOfView)
        {
            _transform = transform;
            _detectionRadius = detectionRadius;
            _fieldOfView = fieldOfView;

            Debug.Log($"[TargetDetector] Создан для {transform.name}. Radius: {detectionRadius}, FOV: {fieldOfView}");
        }

        public void Tick()
        {
            _timeSinceLastUpdate += Time.deltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                _timeSinceLastUpdate = 0f;
                UpdateTarget();
            }
        }

        private void UpdateTarget()
        {
            int playerLayer = LayerMask.GetMask("Player");
            Collider[] hits = Physics.OverlapSphere(_transform.position, _detectionRadius, playerLayer);
            IDamageAble closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<IDamageAble>(out var target))
                {
                    continue;
                }

                Vector3 directionToTarget = (hit.transform.position - _transform.position).normalized;
                float angleToTarget = Vector3.Angle(_transform.forward, directionToTarget);

                if (angleToTarget > _fieldOfView / 2f)
                {
                    continue;
                }

                Vector3 rayOrigin = _transform.position + Vector3.up * 1f;
                if (Physics.Raycast(rayOrigin, directionToTarget, out RaycastHit rayHit, _detectionRadius))
                {
                    if (rayHit.collider != hit)
                    {
                        continue;
                    }
                }

                float distance = Vector3.Distance(_transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            if (_currentTarget != closestTarget)
            {
                if (closestTarget != null)
                {
                    var targetMono = closestTarget as MonoBehaviour;
                    Debug.Log($"<color=yellow>[TargetDetector] {_transform.name} - ЦЕЛЬ НАЙДЕНА: {targetMono?.gameObject.name}</color>");
                }
                else if (_currentTarget != null)
                {
                    Debug.Log($"[TargetDetector] {_transform.name} - Цель потеряна");
                }
            }

            _currentTarget = closestTarget;
        }

        public IDamageAble GetCurrentTarget()
        {
            return _currentTarget;
        }

        public bool HasTarget()
        {
            return _currentTarget != null;
        }
    }
}
