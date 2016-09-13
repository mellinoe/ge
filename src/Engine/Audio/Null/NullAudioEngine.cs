using System.Numerics;

namespace Engine.Audio.Null
{
    public class NullAudioEngine : AudioEngine
    {
        public override AudioResourceFactory ResourceFactory { get; }

        public NullAudioEngine()
        {
            ResourceFactory = new NullAudioResourceFactory();
        }

        public override void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
        }

        public override void SetListenerPosition(Vector3 position)
        {
        }
    }
}
