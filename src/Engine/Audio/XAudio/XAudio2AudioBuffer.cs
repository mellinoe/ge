using SharpDX.XAudio2;
using SharpDX;
using System;

namespace Engine.Audio.XAudio
{
    internal class XAudio2AudioBuffer : AudioBuffer
    {
        public DataStream DataStream { get; private set; }
        public BufferAudioFormat Format { get; private set; }
        public int Frequency { get; private set; }
        public int SizeInBytes { get; private set; }
        public int TotalSamples
        {
            get
            {
                int bytesPerSample = GetBytesPerSample(Format);
                return SizeInBytes / bytesPerSample;
            }
        }

        private int GetBytesPerSample(BufferAudioFormat format)
        {
            switch (format)
            {
                case BufferAudioFormat.Mono8:
                    return 1;
                case BufferAudioFormat.Mono16:
                    return 2;
                case BufferAudioFormat.Stereo8:
                    return 2;
                case BufferAudioFormat.Stereo16:
                    return 4;
                default:
                    throw new InvalidOperationException("Invalid BufferAudioFormat: " + format);
            }
        }

        public XAudio2AudioBuffer()
        {
        }

        public override void BufferData<T>(T[] buffer, BufferAudioFormat format, int sizeInBytes, int frequency)
        {
            DataStream?.Dispose();
            DataStream = new DataStream(sizeInBytes, true, true);
            DataStream.WriteRange(buffer, 0, buffer.Length);
            DataStream.Position = 0;

            Format = format;
            Frequency = frequency;
            SizeInBytes = sizeInBytes;
        }

        public override void Dispose() { }
    }
}