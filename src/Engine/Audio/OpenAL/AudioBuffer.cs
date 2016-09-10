using OpenTK.Audio.OpenAL;
using System;

namespace Engine.Audio.OpenAL
{
    public class OpenALAudioBuffer : AudioBuffer, IDisposable
    {
        public int ID { get; }

        public OpenALAudioBuffer()
        {
            ID = AL.GenBuffer();
        }

        public override void BufferData<T>(T[] buffer, BufferAudioFormat format, int sizeInBytes, int frequency)
        {
            AL.BufferData(ID, MapAudioFormat(format), buffer, sizeInBytes, frequency);
        }

        private ALFormat MapAudioFormat(BufferAudioFormat format)
        {
            switch (format)
            {
                case BufferAudioFormat.Mono8:
                    return ALFormat.Mono8;
                case BufferAudioFormat.Mono16:
                    return ALFormat.Mono16;
                case BufferAudioFormat.Stereo8:
                    return ALFormat.Stereo8;
                case BufferAudioFormat.Stereo16:
                    return ALFormat.Stereo16;
                default:
                    throw new InvalidOperationException("Illegal BufferAudioFormat: " + format);
            }
        }

        public void Dispose()
        {
            AL.DeleteBuffer(ID);
        }
    }
}
