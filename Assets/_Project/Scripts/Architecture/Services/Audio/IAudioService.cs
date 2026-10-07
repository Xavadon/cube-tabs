using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public interface IAudioService : IService
    {
        AudioConfig Config { get; }
        float SoundVolume { get; }
        float MusicVolume { get; }
        void PlayOneShot(AudioClip clip, float volume = 1f);
        void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f);
        void PlayUIClick();
        void PlayMusic(MusicTheme theme);
        void StopMusic();
        void SetSoundVolume(float volume);
        void SetMusicVolume(float volume);
        void SaveVolumeSettings();
    }
}
