using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class AreaWave : MonoBehaviour
    {
        private float _maxRadius;
        private float _duration;
        private float _damage;
        private DamageType _damageType;
        private LayerMask _affectedLayers;

        private float _elapsed;
        private readonly HashSet<Collider> _alreadyHit = new();

        public void Init(float maxRadius, float duration, float damage, DamageType damageType, LayerMask affectedLayers)
        {
            _maxRadius = maxRadius;
            _duration = duration;
            _damage = damage;
            _damageType = damageType;
            _affectedLayers = affectedLayers;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _duration;
            float currentRadius = Mathf.Lerp(0f, _maxRadius, t);

            transform.localScale = Vector3.one * (currentRadius * 2f);

            Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, _affectedLayers);

            foreach (Collider hit in hits)
            {
                if (_alreadyHit.Contains(hit))
                    continue;

                _alreadyHit.Add(hit);

                IDamageAble damageable = hit.GetComponent<IDamageAble>();
                damageable?.ApplyDamage(_damage, hit.ClosestPoint(transform.position), _damageType);
            }
        }
    }
}
