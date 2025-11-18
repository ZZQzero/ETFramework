namespace ET
{
    public static partial class CoroutineLockType
    {
        public const int Mailbox = PackageType.Core * ConstValue.Seed + 1;                   // Mailbox中队列
        
        public const int Location = PackageType.ActorLocation * ConstValue.Seed + 1;                  // location进程上使用
        public const int MessageLocationSender = PackageType.ActorLocation * ConstValue.Seed + 2;       // MessageLocationSender中队列消息 
        
        public const int LoginAccount = PackageType.Login * ConstValue.Seed + 1;       // 登录
        public const int LoginCenterAccount = PackageType.Login * ConstValue.Seed + 2;
        public const int LoginGate = PackageType.Login * ConstValue.Seed + 3;

        public const int DB = PackageType.DB * ConstValue.Seed + 1;
        
        public const int Resources = PackageType.YooAssets * ConstValue.Seed + 1;
        public const int ResourcesLoader = PackageType.YooAssets * ConstValue.Seed + 2;
    }
}