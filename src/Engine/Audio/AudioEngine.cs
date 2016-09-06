using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Engine.Audio
{
    public class AudioEngine
    {
        private readonly AudioContext _context;

        public AudioEngine()
        {
            _context = new AudioContext();
            _context.MakeCurrent();
        }

        public void SetListenerPosition(Vector3 position)
        {
            AL.Listener(ALListener3f.Position, position.X, position.Y, position.Z);
        }

        public void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            OpenTK.Vector3 f = new OpenTK.Vector3(forward.X, forward.Y, forward.Z);
            OpenTK.Vector3 u = new OpenTK.Vector3(up.X, up.Y, up.Z);
            AL.Listener(ALListenerfv.Orientation, ref f, ref u);
        }
    }
}
