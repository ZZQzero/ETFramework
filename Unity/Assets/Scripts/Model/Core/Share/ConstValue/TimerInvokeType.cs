namespace ET
{
    public static partial class TimerInvokeType
    {
        public const int SessionIdleChecker = PackageType.Login * ConstValue.Seed + 1;
        public const int SessionAcceptTimeout = PackageType.Login * ConstValue.Seed + 2;
        public const int AccountSessionCheckOutTimer = PackageType.Login * ConstValue.Seed + 3;
        public const int PlayerOfflineOutTimer = PackageType.Login * ConstValue.Seed + 4;
        
        public const int AITimer = PackageType.AI * ConstValue.Seed + 1;
        
        public const int MoveTimer = PackageType.Move * ConstValue.Seed + 1;
        
        public const int MessageLocationSenderChecker = PackageType.ActorLocation * ConstValue.Seed + 2;
    }
}