using OpenTK.Audio.OpenAL;
using System.Numerics;

namespace Engine.Audio.OpenAL
{
    public class OpenALAudioSource : AudioSource
    {
        public int ID { get; }

        public override float Gain
        {
            get
            {
                float gain;
                AL.GetSource(ID, ALSourcef.Gain, out gain);
                return gain;
            }
            set
            {
                AL.Source(ID, ALSourcef.Gain, value);
            }
        }

        public override bool Looping
        {
            get
            {
                bool looping;
                AL.GetSource(ID, ALSourceb.Looping, out looping);
                return looping;
            }
            set
            {
                AL.Source(ID, ALSourceb.Looping, value);
            }
        }

        public override Vector3 Position
        {
            get
            {
                OpenTK.Vector3 openTKVec;
                AL.GetSource(ID, ALSource3f.Position, out openTKVec);
                return new Vector3(openTKVec.X, openTKVec.Y, openTKVec.Z);
            }
            set
            {
                OpenTK.Vector3 openTKVec = new OpenTK.Vector3(value.X, value.Y, value.Z);
                AL.Source(ID, ALSource3f.Position, ref openTKVec);
            }
        }

        public override Vector3 Direction
        {
            get
            {
                OpenTK.Vector3 openTKVec;
                AL.GetSource(ID, ALSource3f.Direction, out openTKVec);
                return new Vector3(openTKVec.X, openTKVec.Y, openTKVec.Z);
            }
            set
            {
                OpenTK.Vector3 openTKVec = new OpenTK.Vector3(value.X, value.Y, value.Z);
                AL.Source(ID, ALSource3f.Direction, ref openTKVec);
            }
        }

        public override AudioPositionKind PositionKind
        {
            get
            {
                bool sourceRelative;
                AL.GetSource(ID, ALSourceb.SourceRelative, out sourceRelative);
                return sourceRelative ? AudioPositionKind.ListenerRelative : AudioPositionKind.AbsoluteWorld;
            }
            set
            {
                AL.Source(ID, ALSourceb.SourceRelative, value == AudioPositionKind.ListenerRelative ? true : false);
            }
        }

        public OpenALAudioSource()
        {
            ID = AL.GenSource();
        }

        public override void Play(AudioBuffer buffer)
        {
            OpenALAudioBuffer alBuffer = (OpenALAudioBuffer)buffer;
            AL.BindBufferToSource(ID, alBuffer.ID);
            AL.SourcePlay(ID);
        }

        public override void Stop()
        {
            AL.SourceStop(ID);
        }

        public override void Dispose()
        {
            AL.DeleteSource(ID);
        }
    }
}
