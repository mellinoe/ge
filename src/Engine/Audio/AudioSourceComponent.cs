using Engine.Assets;
using OpenTK.Audio.OpenAL;
using System;
using Veldrid.Assets;

namespace Engine.Audio
{
    public class AudioSourceComponent : Component, IDisposable
    {
        private AudioSystem _audioSystem;
        private AssetSystem _assetSystem;

        private AudioSource _source;
        private AudioBuffer _buffer;
        private AssetRef<WaveFile> _clipRef;

        private bool _looping = false;
        private float _gain = 1f;

        public AssetRef<WaveFile> AudioClip
        {
            get { return _clipRef; }
            set
            {
                _clipRef = value;
                if (_assetSystem != null && !value.ID.IsEmpty)
                {
                    GetBufferForRef();
                }
            }
        }

        public bool Looping
        {
            get { return _looping; }
            set { _looping = value; if (_source != null) { _source.Looping = value; } }
        }

        public float Gain
        {
            get { return _gain; }
            set { _gain = value; if (_source != null) { _source.Gain = value; } }
        }

        public void Play()
        {
            if (AudioClip != null && !AudioClip.ID.IsEmpty)
            {
                _source.Play(_buffer);
            }
        }

        public void Stop()
        {
            if (AudioClip != null && !AudioClip.ID.IsEmpty)
            {
                _source.Stop();
            }
        }

        protected override void Attached(SystemRegistry registry)
        {
            _assetSystem = registry.GetSystem<AssetSystem>();
            _audioSystem = registry.GetSystem<AudioSystem>();
            _source = _audioSystem.Engine.ResourceFactory.CreateAudioSource();
            OnTransformChanged(Transform);
            _source.Gain = _gain;
            _source.Looping = Looping;
            if (_clipRef != null && !_clipRef.ID.IsEmpty)
            {
                GetBufferForRef();
            }
        }

        protected override void OnEnabled()
        {
            Transform.TransformChanged += OnTransformChanged;
        }

        protected override void OnDisabled()
        {
            Transform.TransformChanged -= OnTransformChanged;
            _source.Stop();
        }

        protected override void Removed(SystemRegistry registry)
        {
            Dispose();
        }

        private void GetBufferForRef()
        {
            WaveFile wave = _assetSystem.Database.LoadAsset(_clipRef);
            _buffer = _audioSystem.GetAudioBuffer(wave);
        }

        private void OnTransformChanged(Transform t)
        {
            _source.Position = t.Position;
            _source.Direction = t.Forward;
        }

        public void Dispose()
        {
            _source.Dispose();
        }
    }
}
