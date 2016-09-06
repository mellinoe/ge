using OpenTK.Audio.OpenAL;
using System;
using System.Numerics;

namespace Engine.Audio
{
    public class AudioSource : IDisposable
    {
        public int ID { get; }

        public float Gain
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

        public bool Looping
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

        public Vector3 Position
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

        public Vector3 Direction
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

        public AudioPositionKind PositionKind
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

        public AudioSource()
        {
            ID = AL.GenSource();
        }

        public void Play(AudioBuffer buffer)
        {
            AL.BindBufferToSource(ID, buffer.ID);
            AL.SourcePlay(ID);
        }

        public void Stop()
        {
            AL.SourceStop(ID);
        }

        public void Dispose()
        {
            AL.DeleteSource(ID);
        }
    }
}
