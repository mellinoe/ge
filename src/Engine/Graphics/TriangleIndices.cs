using System.Diagnostics;

namespace Engine.Graphics
{
    [DebuggerDisplay("{DebuggerDisplayString,nq}")]
    public struct TriangleIndices
    {
        public readonly int I0;
        public readonly int I1;
        public readonly int I2;

        public TriangleIndices(int i0, int i1, int i2)
        {
            I0 = i0;
            I1 = i1;
            I2 = i2;
        }

        private string DebuggerDisplayString => $"{I0}, {I1}, {I2}";
    }
}
