using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System.Numerics;
using System;

namespace Engine.Audio.OpenAL
{
    public class OpenALAudioEngine : AudioEngine
    {
        private readonly AudioContext _context;

        public override AudioResourceFactory ResourceFactory { get; }

        public OpenALAudioEngine()
        {
            _context = new AudioContext();
            _context.MakeCurrent();
            ResourceFactory = new OpenALResourceFactory();
        }

        public override void SetListenerPosition(Vector3 position)
        {
            AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
        }

        public override void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            OpenTK.Vector3 f = new OpenTK.Vector3(forward.X, forward.Y, forward.Z);
            OpenTK.Vector3 u = new OpenTK.Vector3(up.X, up.Y, up.Z);
            AL.Listener(ALListenerfv.Orientation, ref f, ref u);
        }
    }
}
