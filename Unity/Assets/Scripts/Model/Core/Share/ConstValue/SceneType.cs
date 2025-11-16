namespace ET
{
    [UniqueId]
    public static partial class SceneType
    {
        public const int All = 0;
        public const int Main = PackageType.Core * ConstValue.Seed + 1;
        public const int NetInner = PackageType.Core * ConstValue.Seed + 2;
        public const int NetClient = PackageType.Core * ConstValue.Seed + 3;
        
        public const int Location = PackageType.ActorLocation * ConstValue.Seed + 1;
        
        public const int Realm = PackageType.Login * ConstValue.Seed + 1;
        public const int Gate = PackageType.Login * ConstValue.Seed + 2;
        public const int Router = PackageType.Login * ConstValue.Seed + 3;
        public const int RouterManager = PackageType.Login * ConstValue.Seed + 4;
        
        public const int LoginCenter = PackageType.Login * ConstValue.Seed + 5;
        
        public const int Http = PackageType.StateSync * ConstValue.Seed + 1;
        public const int Map = PackageType.StateSync * ConstValue.Seed + 2;
        public const int Robot = PackageType.StateSync * ConstValue.Seed + 3;

        // 客户端
        public const int Current = PackageType.StateSync * ConstValue.Seed + 21;
        public const int StateSyncView = PackageType.StateSync * ConstValue.Seed + 24;
        
    }
}