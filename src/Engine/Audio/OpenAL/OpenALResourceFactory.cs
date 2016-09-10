namespace Engine.Audio.OpenAL
{
    internal class OpenALResourceFactory : AudioResourceFactory
    {
        public override AudioBuffer CreateAudioBuffer()
        {
            return new OpenALAudioBuffer();
        }

        public override AudioSource CreateAudioSource()
        {
            return new OpenALAudioSource();
        }
    }
}