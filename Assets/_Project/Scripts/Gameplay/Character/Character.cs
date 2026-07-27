using System;
using _Project.Scripts.Architecture;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Components.Health;
using _Project.Scripts.Gameplay.Character.Components.UI;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Inventory;
using _Project.Scripts.Gameplay.Services;
using Game.Scripts.Core.Gameplay.Enemies.Components;
using MinecraftModels.Scripts;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour, IDamageAble, IHealable
    {
        private const float NavMeshAcceleration = 1000f;
        
        public event Action<Character, Vector3, AudioClip[]> OnDamag;
        public event Action<Character, AudioClip[]> OnAttack;
        
        public CharacterType CharacterType { get; private set; }
        public bool IsRanged { get; private set; }
        public CharacterData CharacterDataRef => _characterData;
        public int TierIndex => _tierIndex;
        public float HealthRatio => _health.HealthRatio;
        public CharacterStats Stats => _stats;
        public float CurrentHealth => _health.CurrentHealth;
        public float MaxHealth => _health.MaxHealth;
        
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
        private ICharacterRegistry _registry;
        private HealthBarElement _healthBar;
        private CharacterData _characterData;
        private TierData _tier;
        private CharacterStats _stats;
        private int _tierIndex;
        private bool _stopped;

        public void Initialize(CharacterType characterType, CharacterData characterData, int tierIndex,
            IInputService inputService = null, StatBonus bonus = null)
        {
            CharacterType = characterType;
            _characterData = characterData;
            _tierIndex = tierIndex;
            _tier = characterData.GetTier(tierIndex);
            _stats = new CharacterStats(_tier.Stats, bonus);

            // TODO: LayerMask по строке — вынести в SO (CharacterLayerConfig) или прокинуть через Initialize
            int ownLayer;
            LayerMask targetLayer;

            if (characterType == CharacterType.Ally)
            {
                ownLayer = LayerMask.NameToLayer("Ally");
                targetLayer = LayerMask.GetMask("Enemy");
            }
            else if (characterType == CharacterType.Enemy)
            {
                ownLayer = LayerMask.NameToLayer("Enemy");
                targetLayer = LayerMask.GetMask("Ally");
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null);
            }

            gameObject.layer = ownLayer;

            WeaponData weapon = null;

            if (_tier.WeaponData != null && _tier.WeaponData.Length > 0)
            {
                weapon = _tier.WeaponData[0];
            }

            if (characterData.AnimatorOverride != null)
            {
                _animator.runtimeAnimatorController = characterData.AnimatorOverride;
            }

            _animatorController = new AnimatorController(_animator);

            IsRanged = _tier.BrainData is RangeBrainDataBase;
            _brain = new CharacterBrain(targetLayer, _tier.BrainData, _tier, _stats, _navMeshAgent, _animatorController, transform, weapon, inputService, HandleAttack);
            _movement = new NavMeshMovementComponent(_navMeshAgent, transform, _tier.MoveSpeed);
            _health = new HealthComponent(_stats);

            _navMeshAgent.speed = _tier.MoveSpeed;
            _navMeshAgent.acceleration = NavMeshAcceleration;
            _health.OnDeath += HandleDeath;
            _health.OnDamaged += HandleDamaged;

            ApplySkinAndArmor();
            ApplyWeapons();
        }

        private void Update()
        {
            if (_stopped)
            {
                return;
            }

            if (_brain != null)
            {
                _brain.Tick();
            }
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, DamageType type = DamageType.Physical)
        {
            _health.ApplyDamage(amount, hitPoint, type);
            _animatorController.PlayHitReact();
            _hitEffect.Play();
        }

        public void Heal(float amount)
        {
            _health.Heal(amount);
        }

        public void RefreshStats(StatBonus bonus)
        {
            _stats = new CharacterStats(_tier.Stats, bonus);

            _health.SetStats(_stats);
            _brain.SetStats(_stats);
        }

        public void Stop()
        {
            _stopped = true;
            _movement.Stop();
            _animatorController.ForceIdle();
        }

        public void SetRegistry(ICharacterRegistry registry)
        {
            _registry = registry;
            _registry.OnBattleStopped += Stop;
        }

        public void SetHealthBarPool(HealthBarPool pool)
        {
            _healthBar = pool.Get();
            _healthBar.Bind(transform, _health, pool.GetCamera(), pool);
        }

        private void ApplySkinAndArmor()
        {
            SkinChanger.ChangeSkin(_tier.SkinMaterial);

            if (_tier.ArmorMaterial != null)
            {
                ArmorChanger.ChangeSkin(_tier.ArmorMaterial);
            }
            else
            {
                ArmorChanger.ChangeSkin(_tier.SkinMaterial);
            }
        }

        private void ApplyWeapons()
        {
            if (_tier.WeaponData == null)
            {
                return;
            }

            for (int i = 0; i < _tier.WeaponData.Length; i++)
            {
                _weaponChanger.SetWeapon(_tier.WeaponData[i], i);
            }
        }

        private void HandleDamaged(Vector3 hitPoint)
        {
            OnDamag?.Invoke(this, hitPoint, _tier.HitSounds);
        }

        private void HandleAttack()
        {
            OnAttack?.Invoke(this, _tier.AttackSounds);
        }

        private void HandleDeath()
        {
            TierData tier = _characterData.GetTier(_tierIndex);

            // TODO: ServiceLocator нарушение — прокинуть IPlayerProgressService через Initialize или ICharacterRegistry
            if (CharacterType == CharacterType.Enemy)
            {
                var progress = Project.Get<IPlayerProgressService>();
                if (tier.KillReward > 0)
                    progress.AddGold(tier.KillReward);
                if (tier.ExpReward > 0)
                    progress.AddExp(tier.ExpReward);
            }

            if (tier.DeathAbility != null)
            {
                ExecuteDeathAbility(tier);
            }

            if (_healthBar != null)
            {
                _healthBar.Release();
            }

            if (_registry != null)
            {
                _registry.OnBattleStopped -= Stop;
                _registry.Unregister(this);
            }

            _movement.Stop();

            // TODO: Анимация смерти

            Destroy(gameObject);
        }

        private void ExecuteDeathAbility(TierData tier)
        {
            if (_brain == null)
            {
                return;
            }

            Blackboard blackboard = _brain.Blackboard;
            if (blackboard == null)
            {
                return;
            }

            tier.DeathAbility.Execute(blackboard, _stats.Damage, _stats.DamageType);
        }

#if UNITY_EDITOR
        [Header("Editor skin")]
        [SerializeField]
        private CharacterData _charaSkindata;

        private const int EditorTierIndex = 0;

        [Button]
        public void ApplySkin()
        {
            if (_charaSkindata == null)
            {
                Debug.LogWarning("CharacterData not assigned for editor preview");
                return;
            }

            TierData editorTier = _charaSkindata.GetTier(EditorTierIndex);

            SkinChanger.ChangeSkin(editorTier.SkinMaterial);

            if (editorTier.ArmorMaterial != null)
            {
                ArmorChanger.ChangeSkin(editorTier.ArmorMaterial);
            }
            else
            {
                ArmorChanger.ChangeSkin(editorTier.SkinMaterial);
            }
        }
#endif
    }
}
