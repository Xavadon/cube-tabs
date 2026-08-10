using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Spawning
{
    public class EnemySpawner : SceneSpawner
    {
        private const float PruneInterval = 0.5f;

        [SerializeField] private CharacterData _unit;
        [SerializeField] private int _tier;
        [SerializeField] private int _count = 5;
        [SerializeField] private float _spreadRadius = 6f;
        [SerializeField] private Transform[] _points;

        [Header("Respawn")]
        [SerializeField] private bool _respawn = true;
        [SerializeField] private float _respawnDelay = 2f;

        [SerializeField] private Portal _openOnClear;

        private readonly List<CharacterEntity> _alive = new();

        private ICharacterFactory _factory;
        private ICharacterRegistry _registry;
        private float _nextPrune;
        private bool _pending;
        private bool _started;

        public int AliveCount => _alive.Count;

        public event Action OnCleared;

        public override void Begin()
        {
            if (_unit == null || _started)
                return;

            _started = true;
            _factory = Project.Get<ICharacterFactory>();
            _registry = Project.Get<ICharacterRegistry>();

            Spawn();
        }

        private void Spawn()
        {
            _alive.Clear();

            for (int i = 0; i < _count; i++)
            {
                CharacterEntity enemy = _factory.Create(CharacterType.Enemy, _unit, _tier);
                enemy.SetRegistry(_registry);
                _registry.Register(enemy);
                SpawnPlacement.Place(enemy, PositionFor(i));
                enemy.OnDied += HandleDied;

                _alive.Add(enemy);
            }
        }

        // подчищаем список от уничтоженных: если крип умер мимо события, этап иначе залипнет навсегда
        private void Update()
        {
            if (!_started || _alive.Count == 0 || Time.time < _nextPrune)
                return;

            _nextPrune = Time.time + PruneInterval;

            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == null)
                    _alive.RemoveAt(i);
            }

            if (_alive.Count == 0)
                HandleCleared();
        }

        private void HandleDied(CharacterEntity character)
        {
            character.OnDied -= HandleDied;

            if (!_alive.Remove(character) || _alive.Count > 0)
                return;

            HandleCleared();
        }

        private void HandleCleared()
        {
            if (_openOnClear != null)
                _openOnClear.SetOpen(true);

            OnCleared?.Invoke();

            if (_respawn && !_pending)
                RespawnAfterDelay().Forget();
        }

        private async UniTaskVoid RespawnAfterDelay()
        {
            _pending = true;
            await UniTask.Delay(TimeSpan.FromSeconds(_respawnDelay));
            _pending = false;

            if (this != null)
                Spawn();
        }

        private Vector3 PositionFor(int index)
        {
            if (_points is { Length: > 0 })
                return _points[index % _points.Length].position;

            float angle = index * Mathf.PI * 2f / Mathf.Max(1, _count);
            return transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _spreadRadius;
        }
    }
}
