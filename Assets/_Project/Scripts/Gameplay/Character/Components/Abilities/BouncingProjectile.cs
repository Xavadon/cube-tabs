using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class BouncingProjectile : MonoBehaviour
    {
        private const int MaxSearchHits = 32;

        [field: SerializeField]
        public float Lifetime { get; private set; } = 5f;

        private Transform _target;
        private float _speed;
        private float _damage;
        private DamageType _damageType;
        private int _remainingBounces;
        private float _bounceSearchRadius;
        private LayerMask _targetLayer;

        private readonly Collider[] _searchBuffer = new Collider[MaxSearchHits];
        private readonly HashSet<Transform> _alreadyHit = new();

        public void Init(Transform target, float speed, float damage, DamageType damageType,
            int bounceCount, float bounceSearchRadius, LayerMask targetLayer)
        {
            _target = target;
            _speed = speed;
            _damage = damage;
            _damageType = damageType;
            _remainingBounces = bounceCount;
            _bounceSearchRadius = bounceSearchRadius;
            _targetLayer = targetLayer;

            Destroy(gameObject, Lifetime);
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition = _target.position + Vector3.up;
            Vector3 toTarget = targetPosition - transform.position;
            float distanceThisFrame = _speed * Time.deltaTime;

            if (toTarget.sqrMagnitude <= distanceThisFrame * distanceThisFrame)
            {
                Hit();
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * distanceThisFrame;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Hit()
        {
            IDamageAble damageable = _target.GetComponent<IDamageAble>();
            damageable?.ApplyDamage(_damage, _target.position, _damageType);

            _alreadyHit.Add(_target);

            if (_remainingBounces <= 0 || !TryFindNextTarget(out Transform next))
            {
                // TODO: Возвращать в пул вместо Destroy
                Destroy(gameObject);
                return;
            }

            _remainingBounces--;
            _target = next;
        }

        private bool TryFindNextTarget(out Transform next)
        {
            next = null;

            int hitCount = Physics.OverlapSphereNonAlloc(
                _target.position, _bounceSearchRadius, _searchBuffer, _targetLayer);

            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Transform candidate = _searchBuffer[i].transform;

                if (_alreadyHit.Contains(candidate))
                    continue;

                float dist = Vector3.Distance(_target.position, candidate.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    next = candidate;
                }
            }

            return next != null;
        }
    }
}
