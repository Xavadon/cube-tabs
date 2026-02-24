using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface ICharacterRegistry : IService
    {
        void Register(Character character);
        void Unregister(Character character);
        IReadOnlyList<Character> GetAllies();
    }

    public class CharacterRegistry : ICharacterRegistry
    {
        private readonly List<Character> _allies = new();

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(Character character)
        {
            if (character.CharacterType == CharacterType.Ally)
            {
                _allies.Add(character);
            }
        }

        public void Unregister(Character character)
        {
            _allies.Remove(character);
        }

        public IReadOnlyList<Character> GetAllies() => _allies;
    }
}
