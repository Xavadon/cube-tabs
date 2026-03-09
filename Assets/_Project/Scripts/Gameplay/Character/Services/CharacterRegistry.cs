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
        IReadOnlyList<Character> GetAllies();
        IReadOnlyList<Character> GetEnemies();
        event Action<Character> OnCharacterDied;
    }

    public class CharacterRegistry : ICharacterRegistry
    {
        private readonly List<Character> _allies = new();
        private readonly List<Character> _enemies = new();
        private bool _battleStarted;

        public event Action<Character> OnCharacterDied;

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(Character character)
        {
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

        public IReadOnlyList<Character> GetAllies() => _allies;
        public IReadOnlyList<Character> GetEnemies() => _enemies;
    }
}
