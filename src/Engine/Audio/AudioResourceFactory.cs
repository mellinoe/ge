namespace Engine.Audio
{
    public abstract class AudioResourceFactory
    {
        public abstract AudioSource CreateAudioSource();
        public abstract AudioBuffer CreateAudioBuffer();
    }
}
