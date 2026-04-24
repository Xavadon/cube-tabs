using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Project/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [field: Header("Character Hit")]
        [field: SerializeField] public AudioClip[] AllyHitSounds { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float AllyHitVolume { get; private set; } = 1f;
        [field: SerializeField] public AudioClip[] EnemyHitSounds { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float EnemyHitVolume { get; private set; } = 1f;

        [field: Header("Game Result")]
        [field: SerializeField] public AudioClip VictorySound { get; private set; }
        [field: SerializeField] public AudioClip DefeatSound { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float GameResultVolume { get; private set; } = 1f;

        [field: Header("Throttling - Hit")]
        [field: SerializeField, Range(1, 10)] public int MaxConcurrentAllyHitSounds { get; private set; } = 3;
        [field: SerializeField, Range(1, 10)] public int MaxConcurrentEnemyHitSounds { get; private set; } = 3;
        [field: SerializeField, Range(0f, 0.5f)] public float HitSoundCooldown { get; private set; } = 0.1f;

        [field: Header("Throttling - Attack")]
        [field: SerializeField, Range(1, 10)] public int MaxConcurrentAllyAttackSounds { get; private set; } = 3;
        [field: SerializeField, Range(1, 10)] public int MaxConcurrentEnemyAttackSounds { get; private set; } = 3;
        [field: SerializeField, Range(0f, 0.5f)] public float AttackSoundCooldown { get; private set; } = 0.1f;

        [field: Header("UI")]
        [field: SerializeField] public AudioClip ClickSound { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float ClickVolume { get; private set; } = 1f;

        [field: Header("Settings")]
        [field: SerializeField] public int PoolSize { get; private set; } = 10;
        [field: SerializeField, Range(0f, 1f)] public float MasterVolume { get; private set; } = 1f;

        public AudioClip GetRandomAllyHitSound()
        {
            return GetRandomClip(AllyHitSounds);
        }

        public AudioClip GetRandomEnemyHitSound()
        {
            return GetRandomClip(EnemyHitSounds);
        }

        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }
    }
}
