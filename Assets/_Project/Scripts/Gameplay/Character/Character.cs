using System;
using _Project.Scripts.Architecture;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services;
using Game.Scripts.Core.Gameplay.Enemies.Components;
using MinecraftModels.Scripts;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour, IDamageAble
    {
        public CharacterType CharacterType { get; private set; }

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private NavMeshAgent _navMeshAgent;
        
        [SerializeField]
        private ParticleSystem _hitEffect;

        [SerializeField]
        public SkinChanger SkinChanger;
        
        [SerializeField]
        public SkinChanger ArmorChanger;

        [SerializeField]
        private WeaponChanger _weaponChanger;

        private CharacterBrain _brain;
        private AnimatorConroller _animatorController;
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        private ICharacterRegistry _registry;
        private HealthBarElement _healthBar;
        private CharacterData _characterData;
        
        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            _health.ApplyDamage(amount, hitPoint, type);
            _animatorController.PlayHitReact();
            _hitEffect.Play();
        }

        public void Initialize(CharacterType characterType, CharacterData characterData)
        {
            CharacterType = characterType;
            _characterData = characterData;

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

            if (characterData.ArmorMaterial != null)
            {
                ArmorChanger.ChangeSkin(characterData.ArmorMaterial);
            }
            else
            {
                ArmorChanger.ChangeSkin(characterData.SkinMaterial);
            }

            if (weapon != null)
                _weaponChanger.SetWeapon(weapon);
        }

        private void Update()
        {
            _brain?.Tick();
        }

        public void SetRegistry(ICharacterRegistry registry)
        {
            _registry = registry;
        }

        public void SetHealthBarPool(HealthBarPool pool)
        {
            _healthBar = pool.Get();
            _healthBar.Bind(transform, _health, pool.GetCamera(), pool);
        }

        private void HandleDeath()
        {
            if (CharacterType == CharacterType.Enemy && _characterData.KillReward > 0)
                Project.Get<IPlayerProgressService>().AddGold(_characterData.KillReward);

            _healthBar?.Release();
            _registry?.Unregister(this);
            _movement.Stop();

            // TODO: Анимация смерти

            Destroy(gameObject);
        }
    }
}
