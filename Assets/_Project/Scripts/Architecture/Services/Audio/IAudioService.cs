using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public interface IAudioService : IService
    {
        AudioConfig Config { get; }
        void PlayOneShot(AudioClip clip, float volume = 1f);
        void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f);
    }
}
