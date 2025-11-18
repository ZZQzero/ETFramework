namespace ET
{
    [UniqueId]
    public static partial class ErrorCode
    {
        public const int ERR_Success = 0;

        // 1-11004 是SocketError请看SocketError定义
        //-----------------------------------
        // 100000-100000000是Core层的错误
        
        // 这里配置逻辑层的错误码
        // 100000000以上是逻辑层的错误
        // 200000000以上不抛异常  ErrorCore.IsRpcNeedThrowException
        
        //public const int ErrorExampleException = 100000000 + PackageType.Core * ConstValue.Seed + 1;
        //public const int ErrorExampleNoException = 200000000 + PackageType.Core * ConstValue.Seed + 1;

        #region 100000000

        public const int ERR_WithException = 100000000;
        //------------------------100000000---------------------------------
        public const int ERR_SessionSendOrRecvTimeout = ERR_WithException + PackageType.Core * ConstValue.Seed + 1;
        
        public const int ERR_KcpRouterConnectFail = ERR_WithException + PackageType.Router * ConstValue.Seed + 1;
        public const int ERR_KcpRouterTimeout = ERR_WithException + PackageType.Router * ConstValue.Seed + 2;
        public const int ERR_KcpRouterSame = ERR_WithException + PackageType.Router * ConstValue.Seed + 3;
        public const int ERR_KcpRouterRouterSyncCountTooMuchTimes = ERR_WithException + PackageType.Router * ConstValue.Seed + 4;
        public const int ERR_KcpRouterTooManyPackets = ERR_WithException + PackageType.Router * ConstValue.Seed + 5;
        public const int ERR_KcpRouterSyncCountTooMuchTimes = ERR_WithException + PackageType.Router * ConstValue.Seed + 6;
        
        public const int ERR_NotFoundActor = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 1;
        public const int ERR_RpcFail = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 2;
        public const int ERR_MessageTimeout = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 3;
        public const int ERR_ActorLocationSenderTimeout2 = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 4;
        public const int ERR_ActorLocationSenderTimeout3 = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 5;
        public const int ERR_ActorLocationSenderTimeout4 = ERR_WithException + PackageType.ActorLocation * ConstValue.Seed + 6;
        #endregion
        

        #region 200000000 小于这个Rpc会抛异常，大于这个异常的error需要自己判断处理，也就是说需要处理的错误应该要大于该值
        
        public const int ERR_WithoutException = 200000000;
        //------------------------200000000--------------------------------------
        public const int ERR_Cancel = ERR_WithoutException + PackageType.Core * ConstValue.Seed + 1;
        public const int ERR_Timeout = ERR_WithoutException + PackageType.Core * ConstValue.Seed + 2;
        
        public const int ERR_RequestRepeatedly = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 1;
        public const int ERR_LoginInfoIsNull = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 2;
        public const int ERR_AccountSessionCheckOutTimer = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 3;
        public const int ERR_AccountDifferentLocation = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 4;
        public const int ERR_AccountInBlackList = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 5;
        public const int ERR_AccountNameOrPasswordError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 6;
        public const int ERR_TokenError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 7;
        public const int ERR_LoginGateGameError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 8;
        public const int ERR_ConnectGateKeyError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 9;
        public const int ERR_OtherAccountLogin = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 10;
        public const int ERR_SessionPlayerError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 11;
        public const int ERR_NonePlayerError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 12;
        public const int ERR_EnterGameError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 13;
        public const int ERR_ReEnterGameError = ERR_WithoutException + PackageType.Login * ConstValue.Seed + 14;

        //------------------------------------------------------------------------

        #endregion
        

        public static bool IsRpcNeedThrowException(int error)
        {
            if (error == 0)
            {
                return false;
            }
            if (error > ERR_WithoutException)
            {
                return false;
            }

            return true;
        }
    }
}