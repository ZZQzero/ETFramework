namespace ET
{
    public static partial class LocationType
    {
        public const int Unit = PackageType.Login * ConstValue.Seed + 1;
        public const int User = PackageType.Login * ConstValue.Seed + 2;
        public const int GateSession = PackageType.Login * ConstValue.Seed + 3;
    }
}