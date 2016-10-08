using Engine.Audio.Null;
using Engine.Audio.OpenAL;
using Engine.Audio.XAudio;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Assets;

namespace Engine.Audio
{
    public class AudioSystem : GameSystem
    {
        private readonly AudioEngine _engine;
        private readonly Dictionary<WaveFile, AudioBuffer> _buffers = new Dictionary<WaveFile, AudioBuffer>();

        private readonly AudioSource _freeSoundSource;

        public AudioEngine Engine => _engine;

        public AudioSystem(AudioEngineOptions options)
        {
            _engine = CreateDefaultAudioEngine(options);
            _freeSoundSource = _engine.ResourceFactory.CreateAudioSource();
            _freeSoundSource.Position = new Vector3();
            _freeSoundSource.PositionKind = AudioPositionKind.ListenerRelative;
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
        }

        public void RegisterListener(AudioListener audioListener)
        {
            audioListener.Transform.TransformChanged += OnListenerTransformChanged;
        }

        public void UnregisterListener(AudioListener audioListener)
        {
            audioListener.Transform.TransformChanged -= OnListenerTransformChanged;
        }

        public AudioBuffer GetAudioBuffer(WaveFile wave)
        {
            AudioBuffer buffer;
            if (!_buffers.TryGetValue(wave, out buffer))
            {
                buffer = _engine.ResourceFactory.CreateAudioBuffer();
                buffer.BufferData(wave.Data, wave.Format, wave.SizeInBytes, wave.Frequency);
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
            PlaySound(wave, 1.0f);
        }

        public void PlaySound(WaveFile wave, float volume)
        {
            AudioBuffer buffer = GetAudioBuffer(wave);
            _freeSoundSource.Gain = volume;
            _freeSoundSource.Play(buffer);
        }
    }
}
