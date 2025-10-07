namespace TPSBR
{
    public static class WeaponSizeExtensions
    {
        public static int ToStanceIndex(this WeaponSize size)
        {
            return size switch
            {
                WeaponSize.Unarmed => 0,
                WeaponSize.Light => 1,
                WeaponSize.Throwable => 1,
                WeaponSize.Heavy => 2,
                WeaponSize.Staff => 3,
                _ => 0,
            };
        }

        public static string ToDisplayClass(this WeaponSize size)
        {
            return size switch
            {
                WeaponSize.Unarmed => "UNARMED",
                WeaponSize.Light => "SIDEARM",
                WeaponSize.Heavy => "PRIMARY",
                WeaponSize.Staff => "STAFF",
                WeaponSize.Throwable => "THROWABLE",
                _ => "UNARMED",
            };
        }
    }
}
