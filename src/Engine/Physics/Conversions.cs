namespace Engine.Physics
{
    public static class Conversions
    {
        public static BEPUutilities.Ray ToBEPURay(this Veldrid.Ray ray)
        {
            return new BEPUutilities.Ray(ray.Origin, ray.Direction);
        }
    }
}
