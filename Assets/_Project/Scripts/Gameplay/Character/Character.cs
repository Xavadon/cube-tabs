using System;
using System.Diagnostics;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using ICharacterRegistry = global::_Project.Scripts.Gameplay.Character.Services.ICharacterRegistry;
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
        
        [SerializeField]
        private Animator _animator;
        
        [SerializeField]
        private NavMeshAgent _navMeshAgent;

        [SerializeField]
        public SkinChanger SkinChanger; 
        
        private CharacterBrain _brain; 
        private AnimatorConroller _animatorController;
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        private ICharacterRegistry _registry;
        
        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            _health.ApplyDamage(amount, hitPoint, type);
            _animatorController.PlayHitReact();
        }

        public void Initialize(CharacterType characterType, CharacterData characterData)
        {
            CharacterType = characterType;

            (int ownLayer, LayerMask targetLayer) = characterType switch
            {
                CharacterType.Ally  => (LayerMask.NameToLayer("Ally"),  (LayerMask)LayerMask.GetMask("Enemy")),
                CharacterType.Enemy => (LayerMask.NameToLayer("Enemy"), (LayerMask)LayerMask.GetMask("Ally")),
                _ => throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null)
            };

            gameObject.layer = ownLayer;

            WeaponData weapon;
            
            if (characterData.WeaponData?.Length > 0)
            {
                weapon = characterData.WeaponData[0];
            }
            else
            {
                weapon = null;
            }

            _animatorController = new(_animator);

            _brain = new(targetLayer, characterData.BrainData, _navMeshAgent, _animatorController, transform, weapon);
            _movement = new(_navMeshAgent, transform, characterData.MoveSpeed);
            _health = new(characterData);
            _resistance = new(characterData);

            _navMeshAgent.speed = characterData.MoveSpeed;
            _navMeshAgent.acceleration = 1000f;
            _health.OnDeath += HandleDeath;
            
            SkinChanger.ChangeSkin(characterData.SkinMaterial);
        }

        private void Update()
        {
            _brain?.Tick();
        }

        public void SetRegistry(ICharacterRegistry registry)
        {
            _registry = registry;
        }

        private void HandleDeath()
        {
            _registry?.Unregister(this);
            _movement.Stop();

            // TODO: Анимация смерти

            Destroy(gameObject);
        }
    }
}
