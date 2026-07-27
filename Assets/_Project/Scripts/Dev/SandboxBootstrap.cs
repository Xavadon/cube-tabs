using System;
using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Localization;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.Merchant;
using _Project.Scripts.Gameplay.Merchant.UI;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Dev
{
    public class SandboxBootstrap : MonoBehaviour
    {
        [SerializeField] private CharacterData _playerUnit;
        [SerializeField] private int _tierIndex;
        [SerializeField] private Transform _spawnPoint;

        [Header("Enemy wave")]
        [SerializeField] private CharacterData _enemyUnit;
        [SerializeField] private int _enemyTier;
        [SerializeField] private int _enemyCount = 5;
        [SerializeField] private float _respawnDelay = 2f;

        [Header("Equipment")]
        [SerializeField] private ItemData[] _startingItems;
        [SerializeField] private ItemData[] _startingBackpack;

        private ICharacterSpawner _spawner;
        private ICharacterRegistry _registry;
        private IEquipmentService _equipment;
        private Character _player;
        private bool _respawning;

        private async void Awake()
        {
            await Project.InitializeServicesOnly();

            _spawner = Project.Get<ICharacterSpawner>();
            _registry = Project.Get<ICharacterRegistry>();
            _equipment = Project.Get<IEquipmentService>();

            EquipStartingItems();

            _player = SpawnPlayer();
            _equipment.OnChanged += HandleEquipmentChanged;

            var progress = Project.Get<IPlayerProgressService>();
            var localization = Project.Get<ILocalizationService>();
            var merchantView = FindAnyObjectByType<MerchantView>();

            if (merchantView != null)
                merchantView.Bind(Project.Get<IMerchantService>(), progress, localization);

            var hud = FindAnyObjectByType<HudView>();
            if (hud != null)
            {
                hud.Bind(progress, _equipment, localization, Project.Get<IUnitPreviewService>(), _player);

                if (merchantView != null)
                    hud.OnMerchantClicked += merchantView.Toggle;
            }

            _registry.StartBattle();
            _registry.OnCharacterDied += HandleCharacterDied;
            RespawnWave().Forget();
        }

        private void OnDestroy()
        {
            if (_registry != null)
                _registry.OnCharacterDied -= HandleCharacterDied;

            if (_equipment != null)
                _equipment.OnChanged -= HandleEquipmentChanged;

            Project.Dispose();
        }

        private void EquipStartingItems()
        {
            if (_startingItems != null)
            {
                for (int i = 0; i < _startingItems.Length && i < _equipment.SlotCount; i++)
                {
                    if (_startingItems[i] != null)
                        _equipment.Equip(_startingItems[i], i);
                }
            }

            if (_startingBackpack == null)
                return;

            foreach (ItemData item in _startingBackpack)
                _equipment.AddToBackpack(item);
        }

        private void HandleEquipmentChanged()
        {
            if (_player != null)
                _player.RefreshStats(_equipment.TotalBonus);
        }

        private Character SpawnPlayer()
        {
            var factory = Project.Get<ICharacterFactory>();

            var character = factory.Create(CharacterType.Ally, _playerUnit, _tierIndex);
            character.SetRegistry(_registry);
            _registry.Register(character);

            Vector3 spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
            character.transform.position = spawnPosition;

            return character;
        }

        private async UniTaskVoid RespawnWave()
        {
            if (_enemyUnit == null || _respawning)
                return;

            _respawning = true;

            var entries = new[]
            {
                new SpawnEntry { CharacterData = _enemyUnit, TierIndex = _enemyTier, Count = _enemyCount }
            };

            _spawner.SpawnEnemyWave(entries);

            // ждём, пока вся волна физически заспавнится, иначе смерть первого крипа
            // (Count==0 до появления остальных) тригернет повторный респавн-шторм
            await UniTask.WaitUntil(() => _registry.GetEnemies().Count >= _enemyCount);

            _respawning = false;
        }

        private void HandleCharacterDied(Character character)
        {
            if (_respawning || _registry.GetEnemies().Count > 0)
                return;

            RespawnAfterDelay().Forget();
        }

        private async UniTaskVoid RespawnAfterDelay()
        {
            _respawning = true;
            await UniTask.Delay(TimeSpan.FromSeconds(_respawnDelay));
            _respawning = false;

            RespawnWave().Forget();
        }
    }
}
