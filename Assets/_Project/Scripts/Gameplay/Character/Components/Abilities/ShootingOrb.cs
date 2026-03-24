using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class ShootingOrb : MonoBehaviour
    {
        private ProjectileAbilityDataBase _projectileAbility;
        private float _detectionRadius;
        private float _attackCooldown;
        private float _damage;
        private DamageType _damageType;
        private LayerMask _targetLayer;

        private float _cooldownTimer;
        private readonly Collider[] _hitBuffer = new Collider[32];

        public void Init(
            ProjectileAbilityDataBase projectileAbility,
            float detectionRadius,
            float attackCooldown,
            float damage,
            DamageType damageType,
            LayerMask targetLayer,
            float lifetime)
        {
            _projectileAbility = projectileAbility;
            _detectionRadius = detectionRadius;
            _attackCooldown = attackCooldown;
            _damage = damage;
            _damageType = damageType;
            _targetLayer = targetLayer;

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0f)
                return;

            Transform target = FindNearestTarget();
            if (target == null)
                return;

            ShootAt(target);
            _cooldownTimer = _attackCooldown;
        }

        private Transform FindNearestTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _detectionRadius, _hitBuffer, _targetLayer);

            Transform nearest = null;
            float nearestDistSq = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Transform candidate = _hitBuffer[i].transform;
                float distSq = (candidate.position - transform.position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void ShootAt(Transform target)
        {
            Vector3 spawnPos = transform.position + _projectileAbility.SpawnOffset;
            Projectile projectile = Instantiate(_projectileAbility.Prefab, spawnPos, Quaternion.identity);
            projectile.Init(target, _projectileAbility.Speed, _damage, _damageType);
        }
    }
}
