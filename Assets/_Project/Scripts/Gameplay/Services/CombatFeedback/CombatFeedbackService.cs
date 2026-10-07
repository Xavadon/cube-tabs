using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Camera;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.UI.CombatFeedback;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.CombatFeedback
{
    public interface ICombatFeedbackService : IService
    {
        void BindView(FloatingTextPool floatingTextPool);
    }

    public class CombatFeedbackService : ICombatFeedbackService
    {
        private const string ConfigPath = "Data/CombatFeedbackConfig";
        private const float NoScale = 1f;

        private readonly ICharacterRegistry _characterRegistry;
        private readonly ICameraService _cameraService;

        private CombatFeedbackConfig _config;
        private FloatingTextPool _floatingTextPool;
        private float _nextAbilityShakeTime;

        public CombatFeedbackService(ICharacterRegistry characterRegistry, ICameraService cameraService)
        {
            _characterRegistry = characterRegistry;
            _cameraService = cameraService;
        }

        public UniTask Initialize()
        {
            _config = Resources.Load<CombatFeedbackConfig>(ConfigPath);
            if (_config == null)
            {
                Debug.LogError($"[CombatFeedbackService] CombatFeedbackConfig not found at '{ConfigPath}'");
                return UniTask.CompletedTask;
            }

            _characterRegistry.OnCharacterDamaged += HandleCharacterDamaged;
            _characterRegistry.OnCharacterDied += HandleCharacterDied;
            _characterRegistry.OnAbilityUsed += HandleAbilityUsed;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _characterRegistry.OnCharacterDamaged -= HandleCharacterDamaged;
            _characterRegistry.OnCharacterDied -= HandleCharacterDied;
            _characterRegistry.OnAbilityUsed -= HandleAbilityUsed;
            _floatingTextPool = null;
        }

        public void BindView(FloatingTextPool floatingTextPool)
        {
            _floatingTextPool = floatingTextPool;
        }

        private void HandleCharacterDamaged(Character.Character character, Vector3 hitPoint, float amount, AudioClip[] hitSounds)
        {
            if (_floatingTextPool == null || amount < _config.MinDamageToShow)
            {
                return;
            }

            FloatingTextStyle style;
            if (character.CharacterType == CharacterType.Enemy)
            {
                style = _config.EnemyDamageStyle;
            }
            else if (_config.ShowAllyDamage)
            {
                style = _config.AllyDamageStyle;
            }
            else
            {
                return;
            }

            float scale = NoScale;
            if (amount >= character.MaxHealth * _config.BigHitHealthRatio)
            {
                scale = _config.BigHitScale;
            }

            _floatingTextPool.TryShow(character.transform.position, amount, style, scale, GetJitter());
        }

        private void HandleCharacterDied(Character.Character character)
        {
            Vector3 position = character.transform.position;

            if (character.MaxHealth >= _config.DeathShakeMinMaxHealth)
            {
                _cameraService.AddTrauma(_config.DeathShakeTrauma, position);
            }

            if (_floatingTextPool == null || character.CharacterType != CharacterType.Enemy)
            {
                return;
            }

            TierData tier = character.CharacterDataRef.GetTier(character.TierIndex);
            if (tier.KillReward <= 0)
            {
                return;
            }

            _floatingTextPool.TryShow(position, tier.KillReward, _config.GoldStyle, NoScale, GetJitter());
        }

        private void HandleAbilityUsed(Vector3 position, AudioClip[] sounds, float cameraShake)
        {
            if (cameraShake <= 0f || Time.time < _nextAbilityShakeTime)
            {
                return;
            }

            _nextAbilityShakeTime = Time.time + _config.AbilityShakeCooldown;
            _cameraService.AddTrauma(cameraShake, position);
        }

        private float GetJitter()
        {
            return Random.Range(-_config.HorizontalJitterPixels, _config.HorizontalJitterPixels);
        }
    }
}
