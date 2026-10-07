using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public class AudioService : IAudioService
    {
        private const string ConfigPath = "Audio/AudioConfig";
        private const string SoundVolumeKey = "settings_sound_volume";
        private const string MusicVolumeKey = "settings_music_volume";

        private AudioConfig _config;
        private Pool<AudioSource> _pool;
        private GameObject _poolContainer;
        private MusicPlayer _music;

        public AudioConfig Config => _config;
        public float SoundVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;

        public UniTask Initialize()
        {
            SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

            _config = Resources.Load<AudioConfig>(ConfigPath);
            if (_config == null)
            {
                Debug.LogWarning($"[AudioService] AudioConfig not found at Resources/{ConfigPath}");
                return UniTask.CompletedTask;
            }

            CreatePool();

            _music = new MusicPlayer(_config, _poolContainer.transform);
            _music.SetVolume(MusicVolume);

            Debug.Log($"[AudioService] Initialized with pool size {_config.PoolSize}");
            return UniTask.CompletedTask;
        }

        private void CreatePool()
        {
            _poolContainer = new GameObject("[AudioSourcePool]");
            UnityEngine.Object.DontDestroyOnLoad(_poolContainer);

            _pool = new Pool<AudioSource>(
                createFunc: CreateAudioSource,
                onGet: source => source.gameObject.SetActive(true),
                onReturn: source =>
                {
                    source.Stop();
                    source.clip = null;
                    source.gameObject.SetActive(false);
                },
                preloadCount: _config.PoolSize
            );
        }

        private AudioSource CreateAudioSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(_poolContainer.transform);
            go.SetActive(false);
            return go.AddComponent<AudioSource>();
        }

        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null || _pool == null || SoundVolume <= 0f)
            {
                return;
            }

            var source = _pool.Get();
            source.spatialBlend = 0f;
            PlayAndReturnAsync(source, clip, volume * _config.MasterVolume * SoundVolume).Forget();
        }

        public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null || _pool == null || SoundVolume <= 0f)
            {
                return;
            }

            var source = _pool.Get();
            source.transform.position = position;
            source.spatialBlend = 1f;
            PlayAndReturnAsync(source, clip, volume * _config.MasterVolume * SoundVolume).Forget();
        }

        public void PlayUIClick()
        {
            if (_config == null || _config.ClickSound == null)
            {
                return;
            }

            PlayOneShot(_config.ClickSound, _config.ClickVolume);
        }

        public void PlayMusic(MusicTheme theme)
        {
            if (_music == null)
            {
                return;
            }

            _music.Play(GetPlaylist(theme));
        }

        public void StopMusic()
        {
            if (_music == null)
            {
                return;
            }

            _music.Stop();
        }

        public void SetSoundVolume(float volume)
        {
            SoundVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        }

        public void SetMusicVolume(float volume)
        {
            MusicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);

            if (_music != null)
            {
                _music.SetVolume(MusicVolume);
            }
        }

        public void SaveVolumeSettings()
        {
            PlayerPrefs.Save();
        }

        private string[] GetPlaylist(MusicTheme theme)
        {
            switch (theme)
            {
                case MusicTheme.Menu:
                    return _config.MenuMusic;
                case MusicTheme.Battle:
                    return _config.BattleMusic;
                default:
                    return null;
            }
        }

        private async UniTaskVoid PlayAndReturnAsync(AudioSource source, AudioClip clip, float volume)
        {
            source.clip = clip;
            source.volume = volume;
            source.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(clip.length), ignoreTimeScale: true);

            if (source != null && _pool != null)
            {
                _pool.Return(source);
            }
        }

        public void Dispose()
        {
            if (_music != null)
            {
                _music.Dispose();
                _music = null;
            }

            if (_poolContainer != null)
            {
                UnityEngine.Object.Destroy(_poolContainer);
            }

            _pool = null;
            Debug.Log("[AudioService] Disposed");
        }
    }
}
