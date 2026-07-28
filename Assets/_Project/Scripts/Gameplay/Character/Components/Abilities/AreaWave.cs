using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class AreaWave : MonoBehaviour
    {
        private const int MaxHits = 32;

        [SerializeField]
        private Transform _visual;

        [SerializeField]
        private Transform _telegraph;

        private float _maxRadius;
        private float _duration;
        private float _telegraphDuration;
        private float _damage;
        private DamageType _damageType;
        private LayerMask _affectedLayers;

        private float _elapsed;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];
        private readonly HashSet<Collider> _alreadyHit = new();

        public void Init(float maxRadius, float duration, float damage, DamageType damageType, LayerMask affectedLayers,
            float telegraphDuration = 0f)
        {
            _maxRadius = maxRadius;
            _duration = duration;
            _telegraphDuration = telegraphDuration;
            _damage = damage;
            _damageType = damageType;
            _affectedLayers = affectedLayers;

            Scale(_visual, 0f);
            Scale(_telegraph, telegraphDuration > 0f ? maxRadius : 0f);

            SetActive(_telegraph, telegraphDuration > 0f);
            SetActive(_visual, telegraphDuration <= 0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // пока идёт телеграф, урона нет — игрок должен успеть выйти из круга
            if (_elapsed < _telegraphDuration)
                return;

            if (_telegraph != null && _telegraph.gameObject.activeSelf)
            {
                SetActive(_telegraph, false);
                SetActive(_visual, true);
            }

            float waveElapsed = _elapsed - _telegraphDuration;

            if (waveElapsed >= _duration)
            {
                // TODO: Возвращать в пул вместо Destroy
                Destroy(gameObject);
                return;
            }

            float currentRadius = _maxRadius * (waveElapsed / _duration);
            Scale(_visual, currentRadius);

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, currentRadius, _hitBuffer, _affectedLayers);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];

                if (!_alreadyHit.Add(hit))
                    continue;

                IDamageAble damageable = hit.GetComponent<IDamageAble>();
                damageable?.ApplyDamage(_damage, hit.ClosestPoint(transform.position), _damageType);
            }
        }

        private void Scale(Transform target, float radius)
        {
            if (target == null)
                return;

            float diameter = radius * 2f;
            Vector3 scale = target == _telegraph
                ? new Vector3(diameter, target.localScale.y, diameter)
                : Vector3.one * diameter;

            target.localScale = scale;
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null)
                target.gameObject.SetActive(active);
        }
    }
}
