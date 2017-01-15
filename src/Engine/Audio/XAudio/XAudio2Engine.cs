using System;
using System.Numerics;
using SharpDX.XAudio2;
using SharpDX.X3DAudio;
using SharpDX.Mathematics.Interop;

namespace Engine.Audio.XAudio
{
    public class XAudio2Engine : AudioEngine
    {
        private Listener _x3dListener;

        public XAudio2 XAudio2 { get; }
        public X3DAudio X3DAudio { get; }
        public override AudioResourceFactory ResourceFactory { get; }

        public Listener Listener => _x3dListener;

        public event Action ListenerChanged;

        public XAudio2Engine()
        {
#if DEBUG
            XAudio2Flags flags = XAudio2Flags.DebugEngine;
#else
            XAudio2Flags flags = XAudio2Flags.None;
#endif
            XAudio2 = new XAudio2(flags, ProcessorSpecifier.DefaultProcessor);
#if DEBUG
            DebugConfiguration debugConfig = new DebugConfiguration();
            debugConfig.BreakMask = (int)LogType.Warnings;
            debugConfig.TraceMask = (int)
                (LogType.Errors | LogType.Warnings | LogType.Information | LogType.Detail | LogType.ApiCalls
                | LogType.FunctionCalls | LogType.Timing | LogType.Locks | LogType.Memory | LogType.Streaming);
            debugConfig.LogThreadID = new RawBool(true);
            debugConfig.LogFileline = new RawBool(true);
            debugConfig.LogFunctionName = new RawBool(true);
            debugConfig.LogTiming = new RawBool(true);
            XAudio2.SetDebugConfiguration(debugConfig, IntPtr.Zero);
#endif
            XAudio2.CriticalError += (s, e) => Console.WriteLine("XAudio2: Critical Error. " + e.ToString());

            MasteringVoice _masteringVoice = new MasteringVoice(XAudio2);
            X3DAudio = new X3DAudio(SharpDX.Multimedia.Speakers.Stereo);
            ResourceFactory = new XAudio2ResourceFactory(this);
            _x3dListener = new Listener();
            _x3dListener.OrientFront = new RawVector3(0, 0, 1);
            _x3dListener.OrientTop = new RawVector3(0, 1, 0);
        }

        public override void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            _x3dListener.OrientFront = new RawVector3(forward.X, forward.Y, -forward.Z);
            _x3dListener.OrientTop = new RawVector3(up.X, up.Y, -up.Z);
            ListenerChanged?.Invoke();
        }

        public override void SetListenerPosition(Vector3 position)
        {
            _x3dListener.Position = new RawVector3(position.X, position.Y, -position.Z);
            ListenerChanged?.Invoke();
        }
    }
}
