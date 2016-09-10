namespace Engine.Audio
{
    public abstract class AudioBuffer
    {
        public abstract void BufferData<T>(T[] buffer, BufferAudioFormat format, int sizeInBytes, int frequency) where T : struct;
    }
}
