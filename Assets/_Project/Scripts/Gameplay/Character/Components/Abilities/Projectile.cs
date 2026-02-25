using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class Projectile : MonoBehaviour
    {
        [field: SerializeField]
        public float HitDistance { get; private set; } = 0.5f;

        private Transform _target;
        private float _speed;
        private float _damage;
        private DamageType _damageType;
        private float _lifetime = 5f;

        public void Init(Transform target, float speed, float damage, DamageType damageType)
        {
            _target = target;
            _speed = speed;
            _damage = damage;
            _damageType = damageType;

            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 targetPosition = _target.position + Vector3.up;
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * (_speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance <= HitDistance)
            {
                IDamageAble damageable = _target.GetComponent<IDamageAble>();
                damageable?.ApplyDamage(_damage, targetPosition, _damageType);
                Destroy(gameObject);
            }
        }
    }
}
