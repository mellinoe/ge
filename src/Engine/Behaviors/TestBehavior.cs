using ImGuiNET;
using Veldrid.Graphics;

namespace Engine.Behaviors
{
    public class TestBehavior : Behavior
    {
        public TestBehavior() { }

        public override void Update(float deltaSeconds)
        {
            ImGui.PushStyleColor(ColorTarget.Text, RgbaFloat.Pink.ToVector4());
            ImGui.Text("Okay...");
            ImGui.PopStyleColor();
        }
    }
}
