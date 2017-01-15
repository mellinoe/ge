using Engine.Audio.Null;
using Engine.Audio.OpenAL;
using Engine.Audio.XAudio;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Audio
{
    public class AudioSystem : GameSystem, IDisposable
    {
        private const uint InitialFreeSources = 2;

        private readonly AudioEngine _engine;
        private readonly Dictionary<WaveFile, AudioBuffer> _buffers = new Dictionary<WaveFile, AudioBuffer>();

        private readonly List<AudioSource> _activeSoundSources = new List<AudioSource>();
        private readonly List<AudioSource> _freeSoundSources;
        private AudioListener _listener;

        public AudioEngine Engine => _engine;

        public AudioSystem(AudioEngineOptions options)
        {
            _engine = CreateDefaultAudioEngine(options);

            _freeSoundSources = new List<AudioSource>();
            for (uint i = 0; i < InitialFreeSources; i++)
            {
                _freeSoundSources.Add(CreateNewFreeAudioSource());
            }
        }

        private AudioSource GetFreeSource()
        {
            AudioSource source;
            if (_freeSoundSources.Count == 0)
            {
                source = CreateNewFreeAudioSource();
            }
            else
            {
                source = _freeSoundSources[_freeSoundSources.Count - 1];
                _freeSoundSources.RemoveAt(_freeSoundSources.Count - 1);
            }

            return source;
        }

        private AudioSource CreateNewFreeAudioSource()
        {
            AudioSource source = _engine.ResourceFactory.CreateAudioSource();
            source.Position = new Vector3();
            source.PositionKind = AudioPositionKind.ListenerRelative;
            return source;
        }

        private AudioEngine CreateDefaultAudioEngine(AudioEngineOptions options)
        {
            if (options == AudioEngineOptions.UseNullAudio)
            {
                return new NullAudioEngine();
            }
            else if (options == AudioEngineOptions.UseOpenAL || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return new OpenALEngine();
                }
                catch (DllNotFoundException) { }
            }
            else
            {
                return new XAudio2Engine();
            }

            return new NullAudioEngine();
        }

        protected override void UpdateCore(float deltaSeconds)
        {
            for (int i = 0; i < _activeSoundSources.Count; i++)
            {
                AudioSource source = _activeSoundSources[i];
                if (!source.IsPlaying || source.PlaybackPosition >= 1f)
                {
                    _activeSoundSources.Remove(source);
                    _freeSoundSources.Add(source);
                    i--;
                }
            }
        }

        public void RegisterListener(AudioListener audioListener)
        {
            _listener = audioListener;
            audioListener.Transform.TransformChanged += OnListenerTransformChanged;
        }

        public void UnregisterListener(AudioListener audioListener)
        {
            _listener = null;
            audioListener.Transform.TransformChanged -= OnListenerTransformChanged;
        }

        public AudioBuffer GetAudioBuffer(WaveFile wave)
        {
            AudioBuffer buffer;
            if (!_buffers.TryGetValue(wave, out buffer))
            {
                buffer = _engine.ResourceFactory.CreateAudioBuffer();
                buffer.BufferData(wave.Data, wave.Format, wave.SizeInBytes, wave.Frequency);
                _buffers.Add(wave, buffer);
            }

            return buffer;
        }

        private void OnListenerTransformChanged(Transform t)
        {
            _engine.SetListenerPosition(t.Position);
            _engine.SetListenerOrientation(t.Forward, t.Up);
        }

        public void PlaySound(WaveFile wave)
        {
            PlaySound(wave, 1.0f, 1.0f);
        }

        public void PlaySound(AudioBuffer buffer)
        {
            PlaySound(buffer, 1.0f, 1.0f, Vector3.Zero, AudioPositionKind.ListenerRelative);
        }

        public void PlaySound(WaveFile wave, float volume)
        {
            PlaySound(wave, volume, 1f);
        }

        public void PlaySound(WaveFile wave, float volume, float pitch)
        {
            AudioBuffer buffer = GetAudioBuffer(wave);
            PlaySound(buffer, volume, pitch, Vector3.Zero, AudioPositionKind.ListenerRelative);
        }

        public void PlaySound(WaveFile wave, float volume, float pitch, Vector3 position, AudioPositionKind positionKind)
        {
            AudioBuffer buffer = GetAudioBuffer(wave);
            PlaySound(buffer, volume, pitch, position, positionKind);
        }

        public void PlaySound(AudioBuffer buffer, float volume, float pitch, Vector3 position, AudioPositionKind positionKind)
        {
            AudioSource source = GetFreeSource();
            source.Gain = volume;
            source.Pitch = pitch;
            source.Play(buffer);
            source.Position = position;
            source.PositionKind = positionKind;
            _activeSoundSources.Add(source);
        }

        public void Dispose()
        {
            foreach (var source in _activeSoundSources)
            {
                source.Dispose();
            }
            foreach (var source in _freeSoundSources)
            {
                source.Dispose();
            }
            foreach (var kvp in _buffers)
            {
                kvp.Value.Dispose();
            }

            if (_engine is IDisposable)
            {
                ((IDisposable)_engine).Dispose();
            }
        }
    }
}
