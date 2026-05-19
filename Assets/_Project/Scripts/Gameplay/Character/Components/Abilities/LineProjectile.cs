using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class LineProjectile : MonoBehaviour
    {
        private const int MaxHits = 32;

        [field: SerializeField]
        public float HitRadius { get; private set; } = 0.5f;

        [field: SerializeField]
        public float Lifetime { get; private set; } = 5f;

        private Vector3 _direction;
        private float _speed;
        private float _maxDistance;
        private float _damage;
        private DamageType _damageType;
        private LayerMask _targetLayer;

        private float _traveledDistance;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];
        private readonly HashSet<Transform> _alreadyHit = new();

        public void Init(Vector3 direction, float speed, float maxDistance, float damage,
            DamageType damageType, LayerMask targetLayer)
        {
            _direction = direction.normalized;
            _speed = speed;
            _maxDistance = maxDistance;
            _damage = damage;
            _damageType = damageType;
            _targetLayer = targetLayer;

            transform.rotation = Quaternion.LookRotation(_direction);
            Destroy(gameObject, Lifetime);
        }

        private void Update()
        {
            float distanceThisFrame = _speed * Time.deltaTime;

            CheckHits();

            transform.position += _direction * distanceThisFrame;
            _traveledDistance += distanceThisFrame;

            if (_traveledDistance >= _maxDistance)
            {
                Destroy(gameObject);
            }
        }

        private void CheckHits()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, HitRadius, _hitBuffer, _targetLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Transform target = _hitBuffer[i].transform;

                if (_alreadyHit.Contains(target))
                {
                    continue;
                }

                _alreadyHit.Add(target);

                IDamageAble damageable = target.GetComponent<IDamageAble>();
                if (damageable != null)
                {
                    damageable.ApplyDamage(_damage, target.position, _damageType);
                }
            }
        }
    }
}
