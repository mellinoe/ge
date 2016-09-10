namespace Engine.Audio
{
    public class AudioListener : Component
    {
        private AudioSystem _audioSystem;

        public AudioListener()
        {
        }

        protected override void Attached(SystemRegistry registry)
        {
            _audioSystem = registry.GetSystem<AudioSystem>();
        }

        protected override void OnDisabled()
        {
            _audioSystem.UnregisterListener(this);
        }

        protected override void OnEnabled()
        {
            _audioSystem.RegisterListener(this);
        }

        protected override void Removed(SystemRegistry registry)
        {
        }
    }
}
