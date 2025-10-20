namespace ShatterStone
{
    /// <summary>
    /// A cache for the visual node bounds
    /// </summary>
    public struct OreNodeBounds
    {
        public float minX, maxX, minZ, maxZ, centerY;

        public OreNodeBounds(float minX, float maxX, float minZ, float maxZ, float centerY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minZ = minZ;
            this.maxZ = maxZ;
            this.centerY = centerY;
        }
    }
}