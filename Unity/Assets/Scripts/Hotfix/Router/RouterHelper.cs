using System;
using System.Net;

namespace ET
{
    public static partial class RouterHelper
    {
        public static async ETTask<Session> CreateRouterSession(this NetComponent netComponent, IPEndPoint address, string account, string password)
        {
            uint localConn = (uint)(account.GetLongHashCode() ^ password.GetLongHashCode() ^ RandomGenerator.RandUInt32());
            (uint recvLocalConn, IPEndPoint routerAddress) = await GetRouterAddress(netComponent, address, localConn, 0);

            if (recvLocalConn == 0)
            {
                throw new Exception($"get router fail: {netComponent.Root().Id} {address}");
            }

            Session routerSession = netComponent.Create(routerAddress, address, recvLocalConn);
            routerSession.AddComponent<PingComponent>();
            routerSession.AddComponent<RouterCheckComponent>();
            
            return routerSession;
        }
        
        public static async ETTask<(uint, IPEndPoint)> GetRouterAddress(this NetComponent netComponent, IPEndPoint address, uint localConn, uint remoteConn)
        {
            RouterAddressComponent routerAddressComponent = netComponent.Root().GetComponent<RouterAddressComponent>();
            IPEndPoint routerInfo = routerAddressComponent.GetAddress();
            
            if (routerInfo == null)
            {
                throw new Exception("无法获取Router地址");
            }
            
            uint recvLocalConn = await netComponent.Connect(routerInfo, address, localConn, remoteConn);
            return (recvLocalConn, routerInfo);
        }

        private static async ETTask<uint> Connect(this NetComponent netComponent, IPEndPoint routerAddress, IPEndPoint realAddress, uint localConn, uint remoteConn)
        {
            uint synFlag = remoteConn == 0 ? KcpProtocalType.RouterSYN : KcpProtocalType.RouterReconnectSYN;
            long id = (long)(((ulong)localConn << 32) | remoteConn);

            var oldConnector = netComponent.GetChild<RouterConnector>(id);
            if (oldConnector != null)
            {
                netComponent.RemoveChild(id);
            }
            
            using RouterConnector routerConnector = netComponent.AddChildWithId<RouterConnector>(id,true);
            
            uint connectId = RandomGenerator.RandUInt32();
            string addressStr = realAddress.ToString();
            byte[] addressBytes = addressStr.ToByteArray();
            int messageLength = addressBytes.Length + 13;
            
            byte[] sendCache = new byte[messageLength];
            sendCache.WriteTo(0, synFlag);
            sendCache.WriteTo(1, localConn);
            sendCache.WriteTo(5, remoteConn);
            sendCache.WriteTo(9, connectId);
            Array.Copy(addressBytes, 0, sendCache, 13, addressBytes.Length);
            
            TimerComponent timerComponent = netComponent.Root().GetComponent<TimerComponent>();
            const int maxRetries = 15;
            const long RETRY_INTERVAL = 200;
            
            int retryCount = maxRetries;
            long lastSendTimer = 0;

            while (true)
            {
                long timeNow = TimeInfo.Instance.ClientFrameTime();
                if (timeNow - lastSendTimer > RETRY_INTERVAL)
                {
                    if (--retryCount < 0)
                    {
                        Log.Error($"router connect timeout: {localConn} {remoteConn} {routerAddress} {realAddress}");
                        return 0;
                    }
                    
                    lastSendTimer = timeNow;
                    routerConnector.Connect(sendCache, 0, messageLength, routerAddress);
                }

                await timerComponent.WaitFrameAsync();
                
                if (routerConnector.Flag != 0)
                {
                    return localConn;
                }
            }
        }
    }
}