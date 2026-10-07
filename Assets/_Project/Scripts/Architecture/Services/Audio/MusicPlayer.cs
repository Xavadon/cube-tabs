using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public class MusicPlayer
    {
        private const int SourceCount = 2;
        private const float DecodeTimeout = 10f;
        private const int DecodePollInterval = 100;

        private readonly AudioConfig _config;
        private readonly AudioSource[] _sources = new AudioSource[SourceCount];
        private readonly float[] _weights = new float[SourceCount];
        private readonly Dictionary<string[], MusicShuffleBag> _bags = new();

        private string[] _playlist;
        private CancellationTokenSource _cts;
        private int _activeIndex;
        private float _volume = 1f;

        public MusicPlayer(AudioConfig config, Transform parent)
        {
            _config = config;

            var root = new GameObject("[Music]");
            root.transform.SetParent(parent);

            for (int i = 0; i < SourceCount; i++)
            {
                var source = root.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.priority = 0;
                source.volume = 0f;
                _sources[i] = source;
            }
        }

        public void Play(string[] playlist)
        {
            if (playlist == null || playlist.Length == 0)
            {
                Stop();
                return;
            }

            if (playlist == _playlist)
            {
                return;
            }

            _playlist = playlist;
            RunPlaylistAsync(playlist, RestartToken()).Forget();
        }

        public void Stop()
        {
            if (_playlist == null)
            {
                return;
            }

            _playlist = null;
            FadeOutAsync(RestartToken()).Forget();
        }

        public void SetVolume(float volume)
        {
            _volume = volume;
            ApplyVolumes();
        }

        public void Dispose()
        {
            CancelLoop();

            for (int i = 0; i < SourceCount; i++)
            {
                ReleaseSource(i);
            }
        }

        private async UniTaskVoid RunPlaylistAsync(string[] playlist, CancellationToken token)
        {
            MusicShuffleBag bag = GetBag(playlist);
            AudioClip upcoming = null;

            try
            {
                upcoming = await LoadNextAsync(playlist, bag, token);

                while (upcoming != null)
                {
                    AudioSource source = StartOnQuietSource(upcoming);
                    upcoming = null;

                    await FadeAsync(1f, token);
                    ReleaseInactiveSources();

                    upcoming = await LoadNextAsync(playlist, bag, token);
                    await WaitForFadePointAsync(source, token);
                }
            }
            finally
            {
                ReleaseClip(upcoming);
            }
        }

        private async UniTaskVoid FadeOutAsync(CancellationToken token)
        {
            await FadeAsync(0f, token);

            for (int i = 0; i < SourceCount; i++)
            {
                ReleaseSource(i);
            }
        }

        private async UniTask FadeAsync(float activeTarget, CancellationToken token)
        {
            while (StepFade(activeTarget))
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private bool StepFade(float activeTarget)
        {
            float step = Time.unscaledDeltaTime / _config.MusicCrossfadeDuration;
            bool fading = false;

            for (int i = 0; i < SourceCount; i++)
            {
                float target = 0f;
                if (i == _activeIndex)
                {
                    target = activeTarget;
                }

                _weights[i] = Mathf.MoveTowards(_weights[i], target, step);

                if (_weights[i] != target)
                {
                    fading = true;
                }
            }

            ApplyVolumes();
            return fading;
        }

        private async UniTask WaitForFadePointAsync(AudioSource source, CancellationToken token)
        {
            float fadePoint = Mathf.Max(0f, source.clip.length - _config.MusicCrossfadeDuration);
            float lastTime = 0f;

            while (source.time < fadePoint)
            {
                if (source.time < lastTime)
                {
                    return;
                }

                lastTime = source.time;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        private async UniTask<AudioClip> LoadNextAsync(string[] playlist, MusicShuffleBag bag, CancellationToken token)
        {
            for (int attempt = 0; attempt < playlist.Length; attempt++)
            {
                AudioClip clip = await LoadClipAsync(playlist[bag.Next()], token);

                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private async UniTask<AudioClip> LoadClipAsync(string track, CancellationToken token)
        {
            string url = BuildUrl(track);

            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                var handler = (DownloadHandlerAudioClip)request.downloadHandler;
                handler.streamAudio = false;
                handler.compressed = true;

                try
                {
                    await request.SendWebRequest().WithCancellation(token);
                }
                catch (UnityWebRequestException exception)
                {
                    Debug.LogWarning($"[MusicPlayer] Failed to load '{url}': {exception.Message}");
                    return null;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                clip.name = track;
                return await WaitUntilDecodedAsync(clip, token);
            }
        }

        private static async UniTask<AudioClip> WaitUntilDecodedAsync(AudioClip clip, CancellationToken token)
        {
            float deadline = Time.realtimeSinceStartup + DecodeTimeout;

            try
            {
                while (IsDecoding(clip) && Time.realtimeSinceStartup < deadline)
                {
                    await UniTask.Delay(DecodePollInterval, true, PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                ReleaseClip(clip);
                throw;
            }

            if (clip.loadState == AudioDataLoadState.Failed || clip.length <= 0f)
            {
                Debug.LogWarning($"[MusicPlayer] Failed to decode '{clip.name}'");
                ReleaseClip(clip);
                return null;
            }

            return clip;
        }

        private static bool IsDecoding(AudioClip clip)
        {
            if (clip.loadState == AudioDataLoadState.Failed)
            {
                return false;
            }

            return clip.loadState == AudioDataLoadState.Loading || clip.length <= 0f;
        }

        private string BuildUrl(string track)
        {
            string path = $"{Application.streamingAssetsPath}/{_config.MusicFolder}/{track}";

            if (path.Contains("://"))
            {
                return path;
            }

            return new Uri(path).AbsoluteUri;
        }

        private AudioSource StartOnQuietSource(AudioClip clip)
        {
            int quietest = 0;
            for (int i = 1; i < SourceCount; i++)
            {
                if (_weights[i] < _weights[quietest])
                {
                    quietest = i;
                }
            }

            ReleaseSource(quietest);
            _activeIndex = quietest;

            AudioSource source = _sources[quietest];
            source.clip = clip;
            source.Play();
            return source;
        }

        private void ReleaseInactiveSources()
        {
            for (int i = 0; i < SourceCount; i++)
            {
                if (i != _activeIndex)
                {
                    ReleaseSource(i);
                }
            }
        }

        private void ReleaseSource(int index)
        {
            _weights[index] = 0f;

            AudioSource source = _sources[index];
            if (source == null)
            {
                return;
            }

            AudioClip clip = source.clip;
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            ReleaseClip(clip);
        }

        private static void ReleaseClip(AudioClip clip)
        {
            if (clip != null)
            {
                Object.Destroy(clip);
            }
        }

        private void ApplyVolumes()
        {
            float volume = _config.MusicVolume * _volume;

            for (int i = 0; i < SourceCount; i++)
            {
                if (_sources[i] != null)
                {
                    _sources[i].volume = _weights[i] * volume;
                }
            }
        }

        private MusicShuffleBag GetBag(string[] playlist)
        {
            if (!_bags.TryGetValue(playlist, out MusicShuffleBag bag))
            {
                bag = new MusicShuffleBag(playlist.Length);
                _bags.Add(playlist, bag);
            }

            return bag;
        }

        private CancellationToken RestartToken()
        {
            CancelLoop();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

        private void CancelLoop()
        {
            if (_cts == null)
            {
                return;
            }

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
