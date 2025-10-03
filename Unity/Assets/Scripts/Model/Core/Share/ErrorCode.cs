namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_SessionSendOrRecvTimeout = ERR_WithException + PackageType.Core * 1000 + 1;
        
        public const int ERR_KcpRouterConnectFail = ERR_WithException + PackageType.Router * 1000 + 1;
        public const int ERR_KcpRouterTimeout = ERR_WithException + PackageType.Router * 1000 + 2;
        public const int ERR_KcpRouterSame = ERR_WithException + PackageType.Router * 1000 + 3;
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ERR_WithException + PackageType.Router * 1000 + 4;
        public const int ERR_KcpRouterTooManyPackets = ERR_WithException + PackageType.Router * 1000 + 5;
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ERR_WithException + PackageType.Router * 1000 + 6;
        
        public const int ERR_NotFoundActor = ERR_WithException + PackageType.ActorLocation * 1000 + 1;
        public const int ERR_RpcFail = ERR_WithException + PackageType.ActorLocation * 1000 + 2;
        public const int ERR_MessageTimeout = ERR_WithException + PackageType.ActorLocation * 1000 + 3;
        public const int ERR_ActorLocationSenderTimeout2 = ERR_WithException + PackageType.ActorLocation * 1000 + 4;
        public const int ERR_ActorLocationSenderTimeout3 = ERR_WithException + PackageType.ActorLocation * 1000 + 5;
        public const int ERR_ActorLocationSenderTimeout4 = ERR_WithException + PackageType.ActorLocation * 1000 + 6;
        
        public const int ERR_ConnectGateKeyError = ERR_WithException + PackageType.Login * 1000 + 1;
    }
}