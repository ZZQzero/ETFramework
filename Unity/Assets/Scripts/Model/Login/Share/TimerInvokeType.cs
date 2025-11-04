namespace ET
{
    public static partial class TimerInvokeType
    {
        public const int SessionIdleChecker = PackageType.Login * 1000 + 1;
        public const int SessionAcceptTimeout = PackageType.Login * 1000 + 2;
        
        public const int AITimer = PackageType.AI * 1000 + 1;
        
        public const int MoveTimer = PackageType.Move * 1000 + 1;
    }
}