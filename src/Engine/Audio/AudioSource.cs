using System;
using System.Numerics;

namespace Engine.Audio
{
    public delegate void AudioSourcePlaybackCompletedHandler(AudioSource source);

    public abstract class AudioSource : IDisposable
    {
        public abstract float Gain { get; set; }
        public abstract float Pitch { get; set; }
        public abstract bool Looping { get; set; }
        public abstract Vector3 Position { get; set; }
        public abstract Vector3 Direction { get; set; }
        public abstract AudioPositionKind PositionKind { get; set; }
        public abstract void Dispose();
        public abstract void Play(AudioBuffer buffer);
        public abstract void Stop();
        public abstract float PlaybackPosition { get; set; }
        public abstract bool IsPlaying { get; }
    }
}
