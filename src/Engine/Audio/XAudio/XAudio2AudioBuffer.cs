using SharpDX.XAudio2;
using SharpDX;

namespace Engine.Audio.XAudio
{
    internal class XAudio2AudioBuffer : AudioBuffer
    {
        public DataStream DataStream { get; private set; }
        public BufferAudioFormat Format { get; private set; }
        public int Frequency { get; private set; }
        public int SizeInBytes { get; private set; }

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
    }
}