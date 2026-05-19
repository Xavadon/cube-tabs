using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public struct DeathRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public CharacterData CharacterData;
        public int TierIndex;
        public CharacterType CharacterType;
        public float DeathTime;
    }

    public interface ICharacterRegistry : IService
    {
        void Register(Character character);
        void Unregister(Character character);
        void StartBattle();
        void StopBattle();
        IReadOnlyList<Character> GetAllies();
        IReadOnlyList<Character> GetEnemies();
        IReadOnlyList<Character> GetAll();
        bool TryGetDeadAlly(out DeathRecord record);
        bool TryGetDeadEnemy(out DeathRecord record);
        void ConsumeDeathRecord(CharacterType type);
        event Action<Character> OnCharacterDied;
        event Action<Character, Vector3, AudioClip[]> OnCharacterDamaged;
        event Action<Character, AudioClip[]> OnCharacterAttacked;
        event Action<Vector3, AudioClip[]> OnAbilityUsed;
        event Action OnBattleStopped;
        void NotifyAbilityUsed(Vector3 position, AudioClip[] sounds);
    }

    public class CharacterRegistry : ICharacterRegistry
    {
        private const int MaxDeathRecords = 8;

        private readonly List<Character> _allies = new();
        private readonly List<Character> _enemies = new();
        private readonly List<Character> _all = new();
        private readonly Queue<DeathRecord> _deadAllies = new();
        private readonly Queue<DeathRecord> _deadEnemies = new();
        private bool _battleStarted;

        public event Action<Character> OnCharacterDied;
        public event Action<Character, Vector3, AudioClip[]> OnCharacterDamaged;
        public event Action<Character, AudioClip[]> OnCharacterAttacked;
        public event Action<Vector3, AudioClip[]> OnAbilityUsed;
        public event Action OnBattleStopped;

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(Character character)
        {
            _all.Add(character);
            character.OnDamag += HandleCharacterDamag;
            character.OnAttack += HandleCharacterAttack;

            switch (character.CharacterType)
            {
                case CharacterType.Ally:
                    _allies.Add(character);
                    break;
                case CharacterType.Enemy:
                    _enemies.Add(character);
                    break;
            }
        }

        public void Unregister(Character character)
        {
            _all.Remove(character);
            character.OnDamag -= HandleCharacterDamag;
            character.OnAttack -= HandleCharacterAttack;

            switch (character.CharacterType)
            {
                case CharacterType.Ally:
                    _allies.Remove(character);
                    break;
                case CharacterType.Enemy:
                    _enemies.Remove(character);
                    break;
            }

            if (_battleStarted)
            {
                RecordDeath(character);
                OnCharacterDied?.Invoke(character);
            }
        }

        private void HandleCharacterDamag(Character character, Vector3 hitPoint, AudioClip[] hitSounds)
        {
            OnCharacterDamaged?.Invoke(character, hitPoint, hitSounds);
        }

        private void HandleCharacterAttack(Character character, AudioClip[] attackSounds)
        {
            OnCharacterAttacked?.Invoke(character, attackSounds);
        }

        public void NotifyAbilityUsed(Vector3 position, AudioClip[] sounds)
        {
            OnAbilityUsed?.Invoke(position, sounds);
        }

        public void StartBattle()
        {
            _battleStarted = true;
        }

        public void StopBattle()
        {
            _battleStarted = false;

            foreach (var character in _all)
                character.Stop();

            _deadAllies.Clear();
            _deadEnemies.Clear();

            OnBattleStopped?.Invoke();
        }

        public IReadOnlyList<Character> GetAllies() => _allies;
        public IReadOnlyList<Character> GetEnemies() => _enemies;
        public IReadOnlyList<Character> GetAll() => _all;

        public bool TryGetDeadAlly(out DeathRecord record)
        {
            if (_deadAllies.Count > 0)
            {
                record = _deadAllies.Peek();
                return true;
            }
            record = default;
            return false;
        }

        public bool TryGetDeadEnemy(out DeathRecord record)
        {
            if (_deadEnemies.Count > 0)
            {
                record = _deadEnemies.Peek();
                return true;
            }
            record = default;
            return false;
        }

        public void ConsumeDeathRecord(CharacterType type)
        {
            switch (type)
            {
                case CharacterType.Ally when _deadAllies.Count > 0:
                    _deadAllies.Dequeue();
                    break;
                case CharacterType.Enemy when _deadEnemies.Count > 0:
                    _deadEnemies.Dequeue();
                    break;
            }
        }

        private void RecordDeath(Character character)
        {
            IResurrectionService resurrectionService = Project.Get<IResurrectionService>();
            if (resurrectionService != null && resurrectionService.WasResurrected(character))
            {
                return;
            }

            var record = new DeathRecord
            {
                Position = character.transform.position,
                Rotation = character.transform.rotation,
                CharacterData = character.CharacterDataRef,
                TierIndex = character.TierIndex,
                CharacterType = character.CharacterType,
                DeathTime = Time.time
            };

            Queue<DeathRecord> queue;
            if (character.CharacterType == CharacterType.Ally)
            {
                queue = _deadAllies;
            }
            else
            {
                queue = _deadEnemies;
            }

            if (queue.Count >= MaxDeathRecords)
            {
                queue.Dequeue();
            }

            queue.Enqueue(record);
        }
    }
}
