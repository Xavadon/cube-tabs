using System;
using _Project.Scripts.Architecture;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services.Scene;
using _Project.Scripts.Gameplay.UI.Arpg;
using UnityEngine;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Spawning
{
    public class BossSpawner : SceneSpawner
    {
        [SerializeField] private CharacterData _boss;
        [SerializeField] private int _tier;
        [SerializeField] private Portal _openOnDefeat;

        private bool _started;
        private bool _defeated;

        public CharacterEntity Instance { get; private set; }

        public event Action OnDefeated;

        public override void Begin()
        {
            if (_boss == null || _started)
                return;

            _started = true;

            var registry = Project.Get<ICharacterRegistry>();

            Instance = Project.Get<ICharacterFactory>().Create(CharacterType.Enemy, _boss, _tier);
            Instance.SetRegistry(registry);
            registry.Register(Instance);
            SpawnPlacement.Place(Instance, transform.position);
            Instance.OnDied += HandleDied;

            // полосу показываем в момент спавна: босс появляется сильно позже загрузки сцены
            HudView hud = FindAnyObjectByType<HudView>();

            if (hud != null)
                hud.ShowBoss(Instance);
        }

        // страховка: если босс умер мимо события, ловим по исчезновению объекта
        private void Update()
        {
            if (!_started || _defeated || Instance != null)
                return;

            Defeat();
        }

        private void HandleDied(CharacterEntity character)
        {
            character.OnDied -= HandleDied;
            Defeat();
        }

        private void Defeat()
        {
            if (_defeated)
                return;

            _defeated = true;
            Instance = null;

            if (_openOnDefeat != null)
                _openOnDefeat.SetOpen(true);

            OnDefeated?.Invoke();
        }
    }
}
