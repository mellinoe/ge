using System.Diagnostics;

namespace Engine.Graphics
{
    [DebuggerDisplay("{DebuggerDisplayString,nq}")]
    public struct TriangleIndices
    {
        public readonly ushort I0;
        public readonly ushort I1;
        public readonly ushort I2;

        public TriangleIndices(ushort i0, ushort i1, ushort i2)
        {
            I0 = i0;
            I1 = i1;
            I2 = i2;
        }

        private string DebuggerDisplayString => $"{I0}, {I1}, {I2}";
    }
}
