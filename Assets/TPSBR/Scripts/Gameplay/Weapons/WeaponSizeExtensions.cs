namespace TPSBR
{
    public static class WeaponSizeExtensions
    {
        public static int ToStanceIndex(this WeaponSize size)
        {
            return size switch
            {
                WeaponSize.Unarmed => 0,
                WeaponSize.Staff => 1,
                WeaponSize.Consumable => 3,
                WeaponSize.Throwable => 5,
                _ => 0,
            };
        }

        public static string ToDisplayClass(this WeaponSize size)
        {
            return size switch
            {
                WeaponSize.Unarmed => "UNARMED",
                WeaponSize.Staff => "STAFF",
                WeaponSize.Consumable => "CONSUMABLE",
                WeaponSize.Throwable => "THROWABLE",
                _ => "UNARMED",
            };
        }
    }
}
