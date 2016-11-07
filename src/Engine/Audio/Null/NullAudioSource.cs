using System;
using System.Numerics;

namespace Engine.Audio.Null
{
    public class NullAudioSource : AudioSource
    {
        public override Vector3 Direction { get; set; }

        public override float Gain { get; set; }
        public override float Pitch { get; set; }

        public override bool Looping { get; set; }

        public override Vector3 Position { get; set; }

        public override AudioPositionKind PositionKind { get; set; }

        public override float PlaybackPosition { get { return 1f; } set { } }

        public override bool IsPlaying => false;

        public override void Dispose()
        {
        }

        public override void Play(AudioBuffer buffer)
        {
        }

        public override void Stop()
        {
        }
    }
}