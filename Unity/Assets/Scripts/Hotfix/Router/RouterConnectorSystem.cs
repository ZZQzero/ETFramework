using System;
using System.Net;

namespace ET
{
    [EntitySystemOf(typeof(RouterConnector))]
    public static partial class RouterConnectorSystem
    {
        [EntitySystem]
        private static void Awake(this RouterConnector self)
        {
            try
            {
                NetComponent netComponent = self.GetParent<NetComponent>();
                KService kService = (KService)netComponent.AService;
                kService.AddRouterAckCallback(self.Id, (flag) =>
                {
                    // 检查RouterConnector是否已被释放
                    if (self.IsDisposed)
                    {
                        return;
                    }
                    self.Flag = flag;
                });
            }
            catch (Exception e)
            {
                Log.Error($"RouterConnector Awake异常: {self.Id}, {e}");
                // 如果注册失败，标记Flag为错误状态，让Connect方法知道失败
                self.Flag = 0;
            }
        }
        [EntitySystem]
        private static void Destroy(this RouterConnector self)
        {
            NetComponent netComponent = self.GetParent<NetComponent>();
            KService kService = (KService)netComponent.AService;
            kService.RemoveRouterAckCallback(self.Id);
        }

        public static void Connect(this RouterConnector self, byte[] bytes, int index, int length, IPEndPoint ipEndPoint)
        {
            NetComponent netComponent = self.GetParent<NetComponent>();
            KService kService = (KService)netComponent.AService;
            kService.Transport.Send(bytes, index, length, ipEndPoint, ChannelType.Connect);
        }
    }
}