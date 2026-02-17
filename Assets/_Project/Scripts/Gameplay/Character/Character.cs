using System;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using Game.Scripts.Core.Gameplay.Enemies.Components;
using MinecraftModels.Scripts;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour
    {
        //public AbilityContainer Abilities { get; private set; } //TODO
        public CharacterType CharacterType { get; private set; }
        public SkinChanger SkinChanger { get; private set; }
        
        [SerializeField]
        private NavMeshAgent _navMeshAgent;
        
        [SerializeField]
        private CharacterBrain _brain;  //TODO: rework brain
        
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        
        public void Initialize(CharacterType characterType, CharacterData characterData)
        {
            CharacterType = characterType;
            
            _brain = new(characterData.BrainData, _navMeshAgent, transform);
            _movement = new(_navMeshAgent, transform, characterData.MoveSpeed);
            _health = new(characterData);
            _resistance = new(characterData);

            _navMeshAgent.speed = characterData.MoveSpeed;
            _navMeshAgent.stoppingDistance = 2; //_brain.attackdistance
            _navMeshAgent.acceleration = 1000f;
            _health.OnDeath += HandleDeath;
            //TODO skinchange
        }

        private void Update()
        {
            _brain?.Tick();
        }

        private void HandleDeath()
        {
            //Brain.Disable();
            _movement.Stop();

            // TODO: Анимация смерти

            Destroy(gameObject, 3f);
        }
    }
}
