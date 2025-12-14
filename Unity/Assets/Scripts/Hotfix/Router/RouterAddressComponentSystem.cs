using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using Nino.Core;

namespace ET
{
    [EntitySystemOf(typeof(RouterAddressComponent))]
    public static partial class RouterAddressComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RouterAddressComponent self, string address)
        {
            self.Address = address;
            string ip = self.Address.Substring(0, self.Address.LastIndexOf(":"));
            self.AddressFamily = IPAddress.Parse(ip).AddressFamily;
        }

        public static async ETTask GetAllRouter(this RouterAddressComponent self)
        {
            long timeNow = TimeInfo.Instance.ClientFrameTime();
            if (self.Info != null && self.CacheTime > 0 && (timeNow - self.CacheTime) < RouterAddressComponent.CacheValidTime)
            {
                return;
            }
            
            string url = $"http://{self.Address}/get_router?v={RandomGenerator.RandUInt32()}";
            byte[] routerInfo = await HttpClientHelper.GetBytes(url);
                    
            if (routerInfo == null || routerInfo.Length == 0)
            {
                throw new Exception("返回数据为空");
            }
                    
            HttpGetRouterResponse httpGetRouterResponse = NinoDeserializer.Deserialize<HttpGetRouterResponse>(routerInfo);
            self.Info = httpGetRouterResponse;
            self.CacheTime = timeNow;
            RandomGenerator.BreakRank(self.Info.Routers);
            self.RefreshRouterAsync().NoContext();
        }
        
        private static async ETTask RefreshRouterAsync(this RouterAddressComponent self)
        {
            await self.Root().GetComponent<TimerComponent>().WaitAsync(RouterAddressComponent.CacheValidTime);
            if (self.IsDisposed)
            {
                return;
            }
            
            Scene root = self.Root();
            if (root.GetComponent<NetComponent>() == null)
            {
                // 网络组件已清理，停止刷新
                Log.Info($"网络组件已清理，停止刷新路由信息: {self.Address}");
                return;
            }
            await self.GetAllRouter();
        }

        private static IPEndPoint ParseAddress(string address, AddressFamily addressFamily)
        {
            int colonIndex = address.LastIndexOf(':');
            if (colonIndex < 0)
            {
                throw new Exception($"无效的地址格式: {address}");
            }
            
            IPAddress ipAddress = IPAddress.Parse(address.Substring(0, colonIndex));
            if (addressFamily == AddressFamily.InterNetworkV6)
            {
                ipAddress = ipAddress.MapToIPv6();
            }
            int port = int.Parse(address.Substring(colonIndex + 1));
            return new IPEndPoint(ipAddress, port);
        }

        public static IPEndPoint GetAddress(this RouterAddressComponent self)
        {
            if (self.Info == null || self.Info.Routers.Count == 0)
            {
                return null;
            }

            string address = self.Info.Routers[self.RouterIndex++ % self.Info.Routers.Count];
            return ParseAddress(address, self.AddressFamily);
        }
        
        public static IPEndPoint GetRealmAddress(this RouterAddressComponent self, string account)
        {
            if (self.Info == null || self.Info.Realms.Count == 0)
            {
                return null;
            }
            
            int index = account.Mode(self.Info.Realms.Count);
            string address = self.Info.Realms[index];
            return ParseAddress(address, AddressFamily.InterNetwork);
        }
    }
}