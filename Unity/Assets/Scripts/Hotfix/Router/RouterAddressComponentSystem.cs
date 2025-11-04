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
        
        public static async ETTask Init(this RouterAddressComponent self)
        {
            await self.GetAllRouter();
        }

        private static async ETTask GetAllRouter(this RouterAddressComponent self)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 2000;
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    string url = $"http://{self.Address}/get_router?v={RandomGenerator.RandUInt32()}";
                    byte[] routerInfo = await HttpClientHelper.Get(url);
                    
                    // 验证数据有效性
                    if (routerInfo == null || routerInfo.Length == 0)
                    {
                        throw new Exception("返回数据为空");
                    }
                    
                    HttpGetRouterResponse httpGetRouterResponse = NinoDeserializer.Deserialize<HttpGetRouterResponse>(routerInfo);
                    
                    // 验证响应数据
                    if (httpGetRouterResponse == null)
                    {
                        throw new Exception("反序列化失败，返回null");
                    }
                    
                    self.Info = httpGetRouterResponse;
                    
                    // 验证是否有有效数据
                    if (self.Info.Routers.Count == 0 && self.Info.Realms.Count == 0)
                    {
                        throw new Exception("路由列表和Realm列表都为空");
                    }
                    
                    // 打乱顺序
                    RandomGenerator.BreakRank(self.Info.Routers);
                    
                    Log.Info($"成功获取路由信息: Routers={self.Info.Routers.Count}, Realms={self.Info.Realms.Count}");
                    self.WaitTenMinGetAllRouter().NoContext();
                    return; // 成功，退出重试循环
                }
                catch (Exception e)
                {
                    Log.Warning($"获取路由信息失败 (尝试 {retry + 1}/{maxRetries}): {e.Message}");
                    
                    if (retry < maxRetries - 1)
                    {
                        // 等待后重试
                        await self.Root().GetComponent<TimerComponent>().WaitAsync(retryDelayMs);
                    }
                    else
                    {
                        // 最后一次重试失败
                        Log.Error($"获取路由信息最终失败，已重试{maxRetries}次: {e}");
                        // 可以设置默认值或抛出异常，这里选择记录错误但不阻塞
                        // 如果Info为null，后续调用会失败，但不会崩溃
                    }
                }
            }
        }
        
        // 等10分钟再获取一次
        public static async ETTask WaitTenMinGetAllRouter(this RouterAddressComponent self)
        {
            await self.Root().GetComponent<TimerComponent>().WaitAsync(5 * 60 * 1000);
            if (self.IsDisposed)
            {
                return;
            }
            await self.GetAllRouter();
        }

        public static IPEndPoint GetAddress(this RouterAddressComponent self)
        {
            if (self.Info == null || self.Info.Routers.Count == 0)
            {
                Log.Warning("路由信息为空或路由列表为空，无法获取路由地址");
                return null;
            }

            string address = self.Info.Routers[self.RouterIndex++ % self.Info.Routers.Count];
            Log.Info($"get router address: {self.RouterIndex - 1} {address}");
            string[] ss = address.Split(':');
            IPAddress ipAddress = IPAddress.Parse(ss[0]);
            if (self.AddressFamily == AddressFamily.InterNetworkV6)
            { 
                ipAddress = ipAddress.MapToIPv6();
            }
            return new IPEndPoint(ipAddress, int.Parse(ss[1]));
        }
        
        public static IPEndPoint GetRealmAddress(this RouterAddressComponent self, string account)
        {
            if (self.Info == null || self.Info.Realms.Count == 0)
            {
                Log.Warning("路由信息为空或Realm列表为空，无法获取Realm地址");
                return null;
            }
            
            int v = account.Mode(self.Info.Realms.Count);
            string address = self.Info.Realms[v];
            
            IPAddress ipAddress = IPAddress.Parse(address.Substring(0, address.LastIndexOf(":")));
            int port = int.Parse(address.Substring(address.LastIndexOf(":") + 1));
            //if (self.IPAddress.AddressFamily == AddressFamily.InterNetworkV6)
            //{ 
            //    ipAddress = ipAddress.MapToIPv6();
            //}
            return new IPEndPoint(ipAddress, port);
        }
    }
}