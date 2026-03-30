using System;
using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services;
using Game.Scripts.Core.Gameplay.Enemies.Components;
using MinecraftModels.Scripts;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour, IDamageAble, IHealable
    {
        public CharacterType CharacterType { get; private set; }
        public bool IsRanged { get; private set; }

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
        private AnimatorController _animatorController;
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        private ICharacterRegistry _registry;
        private HealthBarElement _healthBar;
        private CharacterData _characterData;
        private int _tierIndex;

        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            _health.ApplyDamage(amount, hitPoint, type);
            _animatorController.PlayHitReact();
            _hitEffect.Play();
        }

        public float HealthRatio => _health.HealthRatio;
        public void Heal(float amount) => _health.Heal(amount);

        public void Initialize(CharacterType characterType, CharacterData characterData, int tierIndex,
            IInputService inputService = null)
        {
            CharacterType = characterType;
            _characterData = characterData;
            _tierIndex = tierIndex;

            var tier = characterData.GetTier(tierIndex);

            (int ownLayer, LayerMask targetLayer) = characterType switch
            {
                CharacterType.Ally  => (LayerMask.NameToLayer("Ally"),  (LayerMask)LayerMask.GetMask("Enemy")),
                CharacterType.Enemy => (LayerMask.NameToLayer("Enemy"), (LayerMask)LayerMask.GetMask("Ally")),
                _ => throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null)
            };

            gameObject.layer = ownLayer;

            WeaponData weapon = null;

            if (tier.WeaponData is { Length: > 0 })
                weapon = tier.WeaponData[0];

            if (characterData.AnimatorOverride != null)
                _animator.runtimeAnimatorController = characterData.AnimatorOverride;

            _animatorController = new(_animator);

            IsRanged = tier.BrainData is RangeBrainDataBase;
            _brain = new(targetLayer, tier.BrainData, tier, _navMeshAgent, _animatorController, transform, weapon, inputService);
            _movement = new(_navMeshAgent, transform, tier.MoveSpeed);
            _health = new(tier);
            _resistance = new(tier);

            _navMeshAgent.speed = tier.MoveSpeed;
            _navMeshAgent.acceleration = 1000f;
            _health.OnDeath += HandleDeath;

            SkinChanger.ChangeSkin(tier.SkinMaterial);

            if (tier.ArmorMaterial != null)
                ArmorChanger.ChangeSkin(tier.ArmorMaterial);
            else
                ArmorChanger.ChangeSkin(tier.SkinMaterial);

            if (tier.WeaponData != null)
                for (int i = 0; i < tier.WeaponData.Length; i++)
                    _weaponChanger.SetWeapon(tier.WeaponData[i], i);
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
            var tier = _characterData.GetTier(_tierIndex);

            if (CharacterType == CharacterType.Enemy && tier.KillReward > 0)
                Project.Get<IPlayerProgressService>().AddGold(tier.KillReward);

            if (tier.DeathAbility != null)
                ExecuteDeathAbility(tier);

            _healthBar?.Release();
            _registry?.Unregister(this);
            _movement.Stop();

            // TODO: Анимация смерти

            Destroy(gameObject);
        }

        private void ExecuteDeathAbility(TierData tier)
        {
            var blackboard = _brain?.Blackboard;
            if (blackboard == null)
                return;

            tier.DeathAbility.Execute(blackboard, tier.Stats.Damage, tier.Stats.DamageType);
        }
    }
}
