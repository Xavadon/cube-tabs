using System;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class Projectile : MonoBehaviour
    {
        [field: SerializeField]
        public float HitDistance { get; private set; } = 0.5f;

        [field: SerializeField]
        public float Lifetime { get; private set; } = 5f;

        private Transform _target;
        private Vector3 _lastTargetPosition;
        private float _speed;
        private float _damage;
        private DamageType _damageType;
        private Action<Vector3> _onHit;

        public void Init(Transform target, float speed, float damage, DamageType damageType,
            Action<Vector3> onHit = null)
        {
            _target = target;
            _speed = speed;
            _damage = damage;
            _damageType = damageType;
            _onHit = onHit;
            _lastTargetPosition = target != null ? target.position + Vector3.up : transform.position + transform.forward;

            Destroy(gameObject, Lifetime);
        }

        private void Update()
        {
            float distanceThisFrame = _speed * Time.deltaTime;

            if (_target != null)
                _lastTargetPosition = _target.position + Vector3.up;

            Vector3 toTarget = _lastTargetPosition - transform.position;

            if (toTarget.sqrMagnitude <= distanceThisFrame * distanceThisFrame)
            {
                Hit(_lastTargetPosition);
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * distanceThisFrame;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Hit(Vector3 hitPoint)
        {
            if (_target != null)
            {
                IDamageAble damageable = _target.GetComponent<IDamageAble>();
                damageable?.ApplyDamage(_damage, hitPoint, _damageType);
            }

            _onHit?.Invoke(hitPoint);
            // TODO: Возвращать в пул вместо Destroy
            Destroy(gameObject);
        }
    }
}
