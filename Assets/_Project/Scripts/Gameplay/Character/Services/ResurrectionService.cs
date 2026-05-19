using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Services
{
    public interface IResurrectionService : IService
    {
        void SetHealthBarPool(HealthBarPool pool);
        bool TryResurrectAllAllies(Vector3 spawnPosition, Quaternion rotation);
        bool TryResurrectAllEnemies(Vector3 spawnPosition, Quaternion rotation);
        bool WasResurrected(Character character);
    }

    public class ResurrectionService : IResurrectionService
    {
        private readonly ICharacterFactory _factory;
        private readonly ICharacterRegistry _registry;
        private readonly List<DeathRecord> _tempRecords = new();
        private readonly HashSet<Character> _resurrectedCharacters = new();
        private HealthBarPool _healthBarPool;

        public ResurrectionService(ICharacterFactory factory, ICharacterRegistry registry)
        {
            _factory = factory;
            _registry = registry;
            _registry.OnBattleStopped += HandleBattleStopped;
        }

        public UniTask Initialize()
        {
            return UniTask.CompletedTask;
        }

        public void SetHealthBarPool(HealthBarPool pool)
        {
            _healthBarPool = pool;
        }

        public bool WasResurrected(Character character)
        {
            return _resurrectedCharacters.Contains(character);
        }

        public bool TryResurrectAllAllies(Vector3 spawnPosition, Quaternion rotation)
        {
            return TryResurrectAll(CharacterType.Ally, spawnPosition, rotation);
        }

        public bool TryResurrectAllEnemies(Vector3 spawnPosition, Quaternion rotation)
        {
            return TryResurrectAll(CharacterType.Enemy, spawnPosition, rotation);
        }

        private bool TryResurrectAll(CharacterType type, Vector3 spawnPosition, Quaternion rotation)
        {
            CollectDeathRecords(type);

            if (_tempRecords.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < _tempRecords.Count; i++)
            {
                DeathRecord record = _tempRecords[i];
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                Character newCharacter = CreateCharacter(record.CharacterType, record.CharacterData, record.TierIndex);
                PlaceAndRegister(newCharacter, spawnPosition + offset, rotation);
            }

            return true;
        }

        private void CollectDeathRecords(CharacterType type)
        {
            _tempRecords.Clear();

            while (TryConsumeDeathRecord(type, out DeathRecord record))
            {
                _tempRecords.Add(record);
            }
        }

        private bool TryConsumeDeathRecord(CharacterType type, out DeathRecord record)
        {
            bool hasRecord;
            if (type == CharacterType.Ally)
            {
                hasRecord = _registry.TryGetDeadAlly(out record);
            }
            else
            {
                hasRecord = _registry.TryGetDeadEnemy(out record);
            }

            if (hasRecord)
            {
                _registry.ConsumeDeathRecord(type);
            }

            return hasRecord;
        }

        private Character CreateCharacter(CharacterType type, CharacterData data, int tierIndex)
        {
            return _factory.Create(type, data, tierIndex);
        }

        private void PlaceAndRegister(Character character, Vector3 position, Quaternion rotation)
        {
            if (character == null)
            {
                return;
            }

            character.transform.SetPositionAndRotation(position, rotation);
            character.SetRegistry(_registry);
            _resurrectedCharacters.Add(character);

            if (_healthBarPool != null)
            {
                character.SetHealthBarPool(_healthBarPool);
            }

            _registry.Register(character);
        }

        private void HandleBattleStopped()
        {
            _resurrectedCharacters.Clear();
        }
    }
}
