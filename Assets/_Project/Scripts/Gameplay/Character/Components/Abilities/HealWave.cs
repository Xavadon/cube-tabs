using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class HealWave : MonoBehaviour
    {
        private const int MaxHits = 32;

        private float _maxRadius;
        private float _duration;
        private float _healAmount;
        private LayerMask _affectedLayers;

        private float _elapsed;
        private readonly Collider[] _hitBuffer = new Collider[MaxHits];
        private readonly HashSet<Collider> _alreadyHealed = new();

        public void Init(float maxRadius, float duration, float healAmount, LayerMask affectedLayers)
        {
            _maxRadius = maxRadius;
            _duration = duration;
            _healAmount = healAmount;
            _affectedLayers = affectedLayers;

            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                // TODO: Возвращать в пул вместо Destroy
                Destroy(gameObject);
                return;
            }

            float t = _elapsed / _duration;
            float currentRadius = _maxRadius * t;

            transform.localScale = Vector3.one * (currentRadius * 2f);

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, currentRadius, _hitBuffer, _affectedLayers);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];

                if (!_alreadyHealed.Add(hit))
                    continue;

                if (hit.TryGetComponent(out IHealable healable))
                    healable.Heal(_healAmount);
            }
        }
    }
}
