using System;

namespace Engine.Graphics
{
    public interface GraphicsPreferencesProvider
    {
        float RenderQuality { get; }
        InitialWindowStatePreference WindowStatePreference { get; }
        GraphicsBackEndPreference BackEndPreference { get; }
    }

    public enum GraphicsBackEndPreference
    {
        None,
        Direct3D11,
        OpenGL
    }

    public enum InitialWindowStatePreference
    {
        Normal,
        ExclusiveFullScreen,
        BorderlessFullScreen
    }

    public class DefaultGraphicsPreferencesProvider : GraphicsPreferencesProvider
    {
        public GraphicsBackEndPreference BackEndPreference => GraphicsBackEndPreference.None;
        public float RenderQuality => 1f;
        public InitialWindowStatePreference WindowStatePreference => InitialWindowStatePreference.BorderlessFullScreen;
    }

    public static class GraphicsPreferencesUtil
    {
        public static Veldrid.Platform.WindowState MapPreferencesState(InitialWindowStatePreference state)
        {
            switch (state)
            {
                case InitialWindowStatePreference.Normal:
                    return Veldrid.Platform.WindowState.Normal;
                case InitialWindowStatePreference.ExclusiveFullScreen:
                    return Veldrid.Platform.WindowState.FullScreen;
                case InitialWindowStatePreference.BorderlessFullScreen:
                    return Veldrid.Platform.WindowState.BorderlessFullScreen;
                default:
                    throw new InvalidOperationException("Illegal InitialWindowStatePreference: " + state);
            }
        }
    }
}
