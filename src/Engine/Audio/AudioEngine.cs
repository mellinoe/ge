using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.Numerics;

namespace Engine.Audio
{
    public abstract class AudioEngine
    {
        public abstract void SetListenerPosition(Vector3 position);
        public abstract void SetListenerOrientation(Vector3 forward, Vector3 up);
        public abstract AudioResourceFactory ResourceFactory { get; }
    }
}
