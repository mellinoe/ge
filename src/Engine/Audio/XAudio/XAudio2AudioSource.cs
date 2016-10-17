using System.Numerics;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.Mathematics.Interop;

namespace Engine.Audio.XAudio
{
    public class XAudio2AudioSource : AudioSource
    {
        private readonly XAudio2Engine _engine;
        private readonly SharpDX.XAudio2.AudioBuffer _audioBuffer;
        private SourceVoice _sourceVoice;

        private readonly Emitter _emitter;
        private AudioPositionKind _positionKind;
        private DspSettings _dspSettings = new DspSettings(1, 2);
        private int _channelCount = 1;

        private static readonly Listener s_centeredListener = CreateCenteredListener();
        private bool _sourcePositionDirty;
        private bool _stereoState;

        public XAudio2AudioSource(XAudio2Engine engine)
        {
            _engine = engine;
            WaveFormat waveFormat = new WaveFormat(44000, 16, 1);
            _sourceVoice = new SourceVoice(_engine.XAudio2, waveFormat);
            _audioBuffer = new SharpDX.XAudio2.AudioBuffer();
            _emitter = new Emitter()
            {
                OrientFront = new RawVector3(0, 0, 1),
                OrientTop = new RawVector3(0, 1, 0),
                CurveDistanceScaler = 1
            };

            engine.ListenerChanged += OnListenerChanged;
        }

        public override Vector3 Direction
        {
            get
            {
                var rawPos = _emitter.OrientFront;
                return new Vector3(rawPos.X, rawPos.Y, -rawPos.Z);
            }
            set
            {
                RawVector3 orientFront = _emitter.OrientFront;
                if (orientFront.X != value.X || orientFront.Y != value.Y || orientFront.Z != -value.Z)
                {
                    _emitter.OrientFront = new RawVector3(value.X, value.Y, -value.Z);
                    SourcePositionShouldChange();
                }
            }
        }

        public override float Gain
        {
            get
            {
                return _sourceVoice.Volume;
            }
            set
            {
                _sourceVoice.SetVolume(value);
            }
        }

        public override bool Looping
        {
            get
            {
                return _audioBuffer.LoopCount == SharpDX.XAudio2.AudioBuffer.LoopInfinite;
            }
            set
            {
                _audioBuffer.LoopCount = value ? SharpDX.XAudio2.AudioBuffer.LoopInfinite : 0;
            }
        }

        public override Vector3 Position
        {
            get
            {
                var rawPos = _emitter.Position;
                return new Vector3(rawPos.X, rawPos.Y, -rawPos.Z);
            }
            set
            {
                RawVector3 emitterPos = _emitter.Position;
                if (emitterPos.X != value.X || emitterPos.Y != value.Y || emitterPos.Z != -value.Z)
                {
                    _emitter.Position = new RawVector3(value.X, value.Y, -value.Z);
                    SourcePositionShouldChange();
                }
            }
        }

        public override AudioPositionKind PositionKind
        {
            get
            {
                return _positionKind;
            }
            set
            {
                if (_positionKind != value)
                {
                    _positionKind = value;
                    SourcePositionShouldChange();
                }
            }
        }

        private void SourcePositionShouldChange()
        {
            if (_sourceVoice.State.BuffersQueued != 0)
            {
                UpdateSourcePosition();
            }
            else
            {
                _sourcePositionDirty = true; // Defer position calculations until audio is actually going to be playing.
            }
        }

        public override void Dispose()
        {
            _sourceVoice.Dispose();
        }

        public override void Play(AudioBuffer buffer)
        {
            XAudio2AudioBuffer xa2Buffer = (XAudio2AudioBuffer)buffer;
            _channelCount = GetChannelCount(xa2Buffer.Format);
            _emitter.ChannelCount = _channelCount;
            if ((_channelCount > 1 && !_stereoState) || (_channelCount == 1 && _stereoState))
            {
                float volume = _sourceVoice.Volume;
                _sourceVoice.DestroyVoice();
                _sourceVoice.Dispose();
                WaveFormat waveFormat = new WaveFormat(xa2Buffer.Frequency, GetChannelCount(xa2Buffer.Format));
                _sourceVoice = new SourceVoice(_engine.XAudio2, waveFormat);
                _sourceVoice.SetVolume(volume);
                _emitter.ChannelAzimuths = new[] { 0.0f };
                _dspSettings = new DspSettings(_channelCount, 2);
                UpdateSourcePosition();
                _stereoState = _channelCount == 2;
            }

            if (_sourcePositionDirty)
            {
                UpdateSourcePosition();
                _sourcePositionDirty = false;
            }

            _audioBuffer.Stream = xa2Buffer.DataStream;
            _audioBuffer.AudioBytes = xa2Buffer.SizeInBytes;
            _audioBuffer.Flags = BufferFlags.EndOfStream;
            _sourceVoice.Stop();
            _sourceVoice.FlushSourceBuffers();
            _sourceVoice.SubmitSourceBuffer(_audioBuffer, null);
            _sourceVoice.Start();
        }

        public override void Stop()
        {
            _sourceVoice.Stop();
        }

        private void OnListenerChanged()
        {
            if (PositionKind == AudioPositionKind.AbsoluteWorld)
            {
                UpdateSourcePosition();
            }
        }

        private void UpdateSourcePosition()
        {
            Listener listener = PositionKind == AudioPositionKind.ListenerRelative ? s_centeredListener : _engine.Listener;
            _engine.X3DAudio.Calculate(
                listener,
                _emitter,
                CalculateFlags.Matrix | CalculateFlags.Doppler,
                _dspSettings);
            _sourceVoice.SetOutputMatrix(_channelCount, 2, _dspSettings.MatrixCoefficients);
            _sourceVoice.SetFrequencyRatio(_dspSettings.DopplerFactor);
        }

        private int GetChannelCount(BufferAudioFormat format)
        {
            if (format == BufferAudioFormat.Mono8 || format == BufferAudioFormat.Mono16)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        private static Listener CreateCenteredListener()
        {
            return new Listener()
            {
                OrientFront = new RawVector3(0, 0, 1),
                OrientTop = new RawVector3(0, 1, 0),
            };
        }
    }
}