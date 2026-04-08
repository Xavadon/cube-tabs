using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface ICharacterRegistry : IService
    {
        void Register(Character character);
        void Unregister(Character character);
        void StartBattle();
        void StopBattle();
        IReadOnlyList<Character> GetAllies();
        IReadOnlyList<Character> GetEnemies();
        IReadOnlyList<Character> GetAll();
        event Action<Character> OnCharacterDied;
        event Action OnBattleStopped;
    }

    public class CharacterRegistry : ICharacterRegistry
    {
        private readonly List<Character> _allies = new();
        private readonly List<Character> _enemies = new();
        private readonly List<Character> _all = new();
        private bool _battleStarted;

        public event Action<Character> OnCharacterDied;
        public event Action OnBattleStopped;

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(Character character)
        {
            _all.Add(character);

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
                OnCharacterDied?.Invoke(character);
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

            OnBattleStopped?.Invoke();
        }

        public IReadOnlyList<Character> GetAllies() => _allies;
        public IReadOnlyList<Character> GetEnemies() => _enemies;
        public IReadOnlyList<Character> GetAll() => _all;
    }
}
