using System;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Services;
using _Project.Scripts.Gameplay.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public interface IGameplayAudioHandler : IService { }

    public class GameplayAudioHandler : IGameplayAudioHandler
    {
        private readonly IAudioService _audioService;
        private readonly ICharacterRegistry _characterRegistry;
        private readonly IGameResultService _gameResultService;

        private int _activeAllyHitSounds;
        private int _activeEnemyHitSounds;
        private int _activeAllyAttackSounds;
        private int _activeEnemyAttackSounds;
        private int _activeAbilitySounds;
        private float _lastHitSoundTime;
        private float _lastAttackSoundTime;
        private float _lastAbilitySoundTime;

        public GameplayAudioHandler(
            IAudioService audioService,
            ICharacterRegistry characterRegistry,
            IGameResultService gameResultService)
        {
            _audioService = audioService;
            _characterRegistry = characterRegistry;
            _gameResultService = gameResultService;
        }

        public UniTask Initialize()
        {
            _characterRegistry.OnCharacterDamaged += HandleCharacterDamaged;
            _characterRegistry.OnCharacterAttacked += HandleCharacterAttacked;
            _characterRegistry.OnAbilityUsed += HandleAbilityUsed;
            _gameResultService.OnGameFinished += HandleGameFinished;

            Debug.Log("[GameplayAudioHandler] Initialized");
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _characterRegistry.OnCharacterDamaged -= HandleCharacterDamaged;
            _characterRegistry.OnCharacterAttacked -= HandleCharacterAttacked;
            _characterRegistry.OnAbilityUsed -= HandleAbilityUsed;
            _gameResultService.OnGameFinished -= HandleGameFinished;

            Debug.Log("[GameplayAudioHandler] Disposed");
        }

        private void HandleCharacterAttacked(Character character, AudioClip[] attackSounds)
        {
            if (attackSounds == null || attackSounds.Length == 0)
            {
                return;
            }

            var config = _audioService.Config;
            if (config == null)
            {
                return;
            }

            bool isAlly = character.CharacterType == CharacterType.Ally;

            if (!CanPlayAttackSound(config, isAlly))
            {
                return;
            }

            var clip = attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)];
            if (clip != null)
            {
                PlayAttackSoundAsync(clip, character.transform.position, isAlly).Forget();
            }
        }

        private bool CanPlayAttackSound(AudioConfig config, bool isAlly)
        {
            int active;
            int max;

            if (isAlly)
            {
                active = _activeAllyAttackSounds;
                max = config.MaxConcurrentAllyAttackSounds;
            }
            else
            {
                active = _activeEnemyAttackSounds;
                max = config.MaxConcurrentEnemyAttackSounds;
            }

            if (active >= max)
            {
                return false;
            }

            if (Time.time - _lastAttackSoundTime < config.AttackSoundCooldown)
            {
                return false;
            }

            return true;
        }

        private async UniTaskVoid PlayAttackSoundAsync(AudioClip clip, Vector3 position, bool isAlly)
        {
            if (isAlly)
            {
                _activeAllyAttackSounds++;
            }
            else
            {
                _activeEnemyAttackSounds++;
            }

            _lastAttackSoundTime = Time.time;

            _audioService.PlayAtPosition(clip, position);

            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), ignoreTimeScale: true);

            if (isAlly)
            {
                _activeAllyAttackSounds--;
            }
            else
            {
                _activeEnemyAttackSounds--;
            }
        }

        private void HandleCharacterDamaged(Character character, Vector3 hitPoint, AudioClip[] hitSounds)
        {
            var config = _audioService.Config;
            if (config == null)
            {
                return;
            }

            bool isAlly = character.CharacterType == CharacterType.Ally;

            if (!CanPlayHitSound(config, isAlly))
            {
                return;
            }

            AudioClip clip;
            float volume;

            if (hitSounds != null && hitSounds.Length > 0)
            {
                clip = hitSounds[UnityEngine.Random.Range(0, hitSounds.Length)];
                if (isAlly)
                {
                    volume = config.AllyHitVolume;
                }
                else
                {
                    volume = config.EnemyHitVolume;
                }
            }
            else if (isAlly)
            {
                clip = config.GetRandomAllyHitSound();
                volume = config.AllyHitVolume;
            }
            else
            {
                clip = config.GetRandomEnemyHitSound();
                volume = config.EnemyHitVolume;
            }

            if (clip != null)
            {
                PlayHitSoundAsync(clip, hitPoint, volume, isAlly).Forget();
            }
        }

        private bool CanPlayHitSound(AudioConfig config, bool isAlly)
        {
            int active;
            int max;

            if (isAlly)
            {
                active = _activeAllyHitSounds;
                max = config.MaxConcurrentAllyHitSounds;
            }
            else
            {
                active = _activeEnemyHitSounds;
                max = config.MaxConcurrentEnemyHitSounds;
            }

            if (active >= max)
            {
                return false;
            }

            if (Time.time - _lastHitSoundTime < config.HitSoundCooldown)
            {
                return false;
            }

            return true;
        }

        private async UniTaskVoid PlayHitSoundAsync(AudioClip clip, Vector3 position, float volume, bool isAlly)
        {
            if (isAlly)
            {
                _activeAllyHitSounds++;
            }
            else
            {
                _activeEnemyHitSounds++;
            }

            _lastHitSoundTime = Time.time;

            _audioService.PlayAtPosition(clip, position, volume);

            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), ignoreTimeScale: true);

            if (isAlly)
            {
                _activeAllyHitSounds--;
            }
            else
            {
                _activeEnemyHitSounds--;
            }
        }

        private void HandleAbilityUsed(Vector3 position, AudioClip[] sounds)
        {
            if (sounds == null || sounds.Length == 0)
            {
                return;
            }

            var config = _audioService.Config;
            if (config == null)
            {
                return;
            }

            if (!CanPlayAbilitySound(config))
            {
                return;
            }

            var clip = sounds[UnityEngine.Random.Range(0, sounds.Length)];
            if (clip != null)
            {
                PlayAbilitySoundAsync(clip, position, config.AbilityVolume).Forget();
            }
        }

        private bool CanPlayAbilitySound(AudioConfig config)
        {
            if (_activeAbilitySounds >= config.MaxConcurrentAbilitySounds)
            {
                return false;
            }

            if (Time.time - _lastAbilitySoundTime < config.AbilitySoundCooldown)
            {
                return false;
            }

            return true;
        }

        private async UniTaskVoid PlayAbilitySoundAsync(AudioClip clip, Vector3 position, float volume)
        {
            _activeAbilitySounds++;
            _lastAbilitySoundTime = Time.time;

            _audioService.PlayAtPosition(clip, position, volume);

            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), ignoreTimeScale: true);

            _activeAbilitySounds--;
        }

        private void HandleGameFinished(GameResultData data)
        {
            var config = _audioService.Config;
            if (config == null)
            {
                return;
            }

            AudioClip clip;
            if (data.Result == GameResult.Victory)
            {
                clip = config.VictorySound;
            }
            else
            {
                clip = config.DefeatSound;
            }

            if (clip != null)
            {
                _audioService.PlayOneShot(clip, config.GameResultVolume);
            }
        }
    }
}
