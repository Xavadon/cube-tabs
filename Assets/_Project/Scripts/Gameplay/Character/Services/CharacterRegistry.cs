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
        private readonly List<Character> _characters = new();

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void Register(Character character)
        {
            //if (character.CharacterType == CharacterType.Ally)
            //{
                _characters.Add(character);
            //}
        }

        public void Unregister(Character character)
        {
            _characters.Remove(character);
        }

        public IReadOnlyList<Character> GetAllies() => _characters;
    }
}
