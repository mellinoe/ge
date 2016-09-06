using OpenTK.Audio.OpenAL;
using System;

namespace Engine.Audio
{
    public class AudioBuffer : IDisposable
    {
        public int ID { get; }

        public AudioBuffer()
        {
            ID = AL.GenBuffer();
        }

        public void BufferData<T>(T[] buffer, BufferAudioFormat format, int sizeInBytes, int frequency) where T : struct
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

    public enum BufferAudioFormat
    {
        Mono8,
        Mono16,
        Stereo8,
        Stereo16
    }
}
