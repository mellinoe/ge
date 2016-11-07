using OpenTK.Audio.OpenAL;
using System.Numerics;
using System;

namespace Engine.Audio.OpenAL
{
    public class OpenALAudioSource : AudioSource
    {
        public OpenALAudioSource()
        {
            ID = AL.GenSource();
            if (ID == 0)
            {
                throw new InvalidOperationException("Too many OpenALAudioSources.");
            }
        }

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

        public override float Pitch
        {
            get
            {
                float pitch;
                AL.GetSource(ID, ALSourcef.Pitch, out pitch);
                return pitch;
            }
            set
            {
                if (value < 0.5 || value > 2.0f)
                {
                    throw new ArgumentOutOfRangeException("Pitch must be between 0.5 and 2.0.");
                }

                AL.Source(ID, ALSourcef.Pitch, value);
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

        /// <summary>
        /// Gets or sets the playback position, as a value between 0.0f (beginning of clip), and 1.0f (end of clip).
        /// </summary>
        public override float PlaybackPosition
        {
            get
            {
                int playbackBytes;
                AL.GetSource(ID, ALGetSourcei.ByteOffset, out playbackBytes);
                int bufferID;
                AL.GetSource(ID, ALGetSourcei.Buffer, out bufferID);
                int totalBufferBytes;
                AL.GetBuffer(bufferID, ALGetBufferi.Size, out totalBufferBytes);
                return (float)playbackBytes / totalBufferBytes;
            }
            set
            {
                int bufferID;
                AL.GetSource(ID, ALGetSourcei.Buffer, out bufferID);
                int totalBufferBytes;
                AL.GetBuffer(bufferID, ALGetBufferi.Size, out totalBufferBytes);
                int newByteOffset = (int)(totalBufferBytes * value);
                AL.Source(ID, ALSourcei.ByteOffset, newByteOffset);
            }
        }

        public override bool IsPlaying
        {
            get
            {
                return AL.GetSourceState(ID) == ALSourceState.Playing;
            }
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
