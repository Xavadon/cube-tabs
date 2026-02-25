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
        private float _speed;
        private float _damage;
        private DamageType _damageType;

        public void Init(Transform target, float speed, float damage, DamageType damageType)
        {
            _target = target;
            _speed = speed;
            _damage = damage;
            _damageType = damageType;

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
                Hit(targetPosition);
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * distanceThisFrame;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Hit(Vector3 hitPoint)
        {
            IDamageAble damageable = _target.GetComponent<IDamageAble>();
            damageable?.ApplyDamage(_damage, hitPoint, _damageType);
            // TODO: Возвращать в пул вместо Destroy
            Destroy(gameObject);
        }
    }
}
