using SharpDX.XAudio2;
using System;

namespace Engine.Audio.XAudio
{
    public class XAudio2ResourceFactory : AudioResourceFactory
    {
        public XAudio2Engine Engine { get; }

        public XAudio2ResourceFactory(XAudio2Engine engine)
        {
            Engine = engine;
        }

        public override AudioBuffer CreateAudioBuffer()
        {
            return new XAudio2AudioBuffer();
        }

        public override AudioSource CreateAudioSource()
        {
            return new XAudio2AudioSource(Engine);
        }
    }
}