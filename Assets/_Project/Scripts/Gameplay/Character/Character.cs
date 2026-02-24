using System;
using System.Diagnostics;
using _Project.Scripts.Gameplay.Character.Components;
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
    public class Character : MonoBehaviour, IDamageAble
    {
        //public AbilityContainer Abilities { get; private set; } //TODO
        public CharacterType CharacterType { get; private set; }
        public SkinChanger SkinChanger { get; private set; }
        
        [SerializeField]
        private Animator _animator;
        
        [SerializeField]
        private NavMeshAgent _navMeshAgent;
        
        private CharacterBrain _brain; 
        private AnimatorConroller _animatorConroller;
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        
        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            _health.ApplyDamage(amount, hitPoint, type);
            _animatorConroller.PlayHitReact();
        }

        public void Initialize(CharacterType characterType, CharacterData characterData)
        {
            CharacterType = characterType;
            
            switch (characterType)
            {
                case CharacterType.Ally:
                    gameObject.layer = LayerMask.NameToLayer("Ally");
                    break;
                case CharacterType.Enemy:
                    gameObject.layer = LayerMask.NameToLayer("Enemy");
                    break;
                case CharacterType.None:
                    throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null);
                default:
                    throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null);
            }

            WeaponData weapon;
            
            if (characterData.WeaponData?.Length > 0)
            {
                weapon = characterData.WeaponData[0];
            }
            else
            {
                weapon = null;
            }

            _animatorConroller = new(_animator);
            
            _brain = new(characterData.BrainData, _navMeshAgent, _animatorConroller, transform, weapon);
            _movement = new(_navMeshAgent, transform, characterData.MoveSpeed);
            _health = new(characterData);
            _resistance = new(characterData);

            _navMeshAgent.speed = characterData.MoveSpeed;
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

            Destroy(gameObject);
        }
    }
}
