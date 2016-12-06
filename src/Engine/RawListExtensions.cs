using BEPUutilities.DataStructures;
using System;

namespace Engine
{
    public static class RawListExtensions
    {
        public static ArraySegment<T> GetArraySegment<T>(this RawList<T> rawList)
        {
            return new ArraySegment<T>(rawList.Elements, 0, rawList.Count);
        }
    }
}
