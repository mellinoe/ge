namespace Engine.Audio
{
    public abstract class AudioBuffer : System.IDisposable
    {
        public abstract void BufferData<T>(T[] buffer, BufferAudioFormat format, int sizeInBytes, int frequency) where T : struct;
        public abstract void Dispose();
    }
}
