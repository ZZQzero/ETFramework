namespace ET
{
    [UniqueId]
    public static partial class SceneType
    {
        public const int All = 0;
        public const int Main = PackageType.Core * 1000 + 1;
        public const int NetInner = PackageType.Core * 1000 + 2;
        public const int NetClient = PackageType.Core * 1000 + 3;
        
        public const int Location = PackageType.ActorLocation * 1000 + 1;
        
        public const int Realm = PackageType.Login * 1000 + 1;
        public const int Gate = PackageType.Login * 1000 + 2;
        public const int Router = PackageType.Login * 1000 + 3;
        public const int RouterManager = PackageType.Login * 1000 + 4;
        
        public const int Http = PackageType.StateSync * 1000 + 1;
        public const int Map = PackageType.StateSync * 1000 + 2;
        public const int Robot = PackageType.StateSync * 1000 + 3;

        // 客户端
        public const int Current = PackageType.StateSync * 1000 + 21;
        public const int StateSyncView = PackageType.StateSync * 1000 + 24;
        
    }
}