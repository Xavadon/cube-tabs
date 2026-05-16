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
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.Character
{
    public class Character : MonoBehaviour, IDamageAble, IHealable
    {
        public CharacterType CharacterType { get; private set; }
        public bool IsRanged { get; private set; }

        public event Action<Character, Vector3, AudioClip[]> OnDamaged;
        public event Action<Character, AudioClip[]> OnAttacked;

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

            var editorTier = _charaSkindata.GetTier(EditorTierIndex);

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

        private CharacterBrain _brain;
        private AnimatorController _animatorController;
        private NavMeshMovementComponent _movement;
        private HealthComponent _health;
        private ResistanceComponent _resistance;
        private ICharacterRegistry _registry;
        private HealthBarElement _healthBar;
        private CharacterData _characterData;
        private TierData _tier;
        private int _tierIndex;
        private bool _stopped;

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
            _tier = characterData.GetTier(tierIndex);

            (int ownLayer, LayerMask targetLayer) = characterType switch
            {
                CharacterType.Ally  => (LayerMask.NameToLayer("Ally"),  (LayerMask)LayerMask.GetMask("Enemy")),
                CharacterType.Enemy => (LayerMask.NameToLayer("Enemy"), (LayerMask)LayerMask.GetMask("Ally")),
                _ => throw new ArgumentOutOfRangeException(nameof(characterType), characterType, null)
            };

            gameObject.layer = ownLayer;

            WeaponData weapon = null;

            if (_tier.WeaponData is { Length: > 0 })
                weapon = _tier.WeaponData[0];

            if (characterData.AnimatorOverride != null)
                _animator.runtimeAnimatorController = characterData.AnimatorOverride;

            _animatorController = new(_animator);

            IsRanged = _tier.BrainData is RangeBrainDataBase;
            _brain = new(targetLayer, _tier.BrainData, _tier, _navMeshAgent, _animatorController, transform, weapon, inputService, HandleAttack);
            _movement = new(_navMeshAgent, transform, _tier.MoveSpeed);
            _health = new(_tier);
            _resistance = new(_tier);

            _navMeshAgent.speed = _tier.MoveSpeed;
            _navMeshAgent.acceleration = 1000f;
            _health.OnDeath += HandleDeath;
            _health.OnDamaged += HandleDamaged;

            SkinChanger.ChangeSkin(_tier.SkinMaterial);

            if (_tier.ArmorMaterial != null)
            {
                ArmorChanger.ChangeSkin(_tier.ArmorMaterial);
            }
            else
            {
                ArmorChanger.ChangeSkin(_tier.SkinMaterial);
            }

            if (_tier.WeaponData != null)
            {
                for (int i = 0; i < _tier.WeaponData.Length; i++)
                {
                    _weaponChanger.SetWeapon(_tier.WeaponData[i], i);
                }
            }
        }

        private void Update()
        {
            if (_stopped)
                return;

            _brain?.Tick();
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

        private void HandleDamaged(Vector3 hitPoint)
        {
            OnDamaged?.Invoke(this, hitPoint, _tier.HitSounds);
        }

        private void HandleAttack()
        {
            OnAttacked?.Invoke(this, _tier.AttackSounds);
        }

        private void HandleDeath()
        {
            var tier = _characterData.GetTier(_tierIndex);

            if (CharacterType == CharacterType.Enemy && tier.KillReward > 0)
                Project.Get<IPlayerProgressService>().AddGold(tier.KillReward);

            if (tier.DeathAbility != null)
                ExecuteDeathAbility(tier);

            _healthBar?.Release();

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
            var blackboard = _brain?.Blackboard;
            if (blackboard == null)
                return;

            tier.DeathAbility.Execute(blackboard, tier.Stats.Damage, tier.Stats.DamageType);
        }
    }
}
