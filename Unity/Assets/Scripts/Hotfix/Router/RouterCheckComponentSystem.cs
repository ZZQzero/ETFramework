using System;
using System.Net;

namespace ET
{
    [EntitySystemOf(typeof(RouterCheckComponent))]
    public static partial class RouterCheckComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RouterCheckComponent self)
        {
            self.CheckAsync().NoContext();
        }

        private static async ETTask CheckAsync(this RouterCheckComponent self)
        {
            Session session = self.GetParent<Session>();
            long instanceId = self.InstanceId;
            Fiber fiber = self.Fiber();
            Scene root = fiber.Root;
            
            IPEndPoint realAddress = session.RemoteAddress;
            NetComponent netComponent = root.GetComponent<NetComponent>();
            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            
            const int maxTotalFailures = 3;
            int totalFailures = 0;
            const long NO_MESSAGE_TIMEOUT = 7 * 1000; // 7秒无消息触发重连
            const long NORMAL_CHECK_INTERVAL = 5 * 1000; // 正常时5秒检查一次
            const long PROBLEM_CHECK_INTERVAL = 1 * 1000; // 有问题时1秒检查一次
            
            while (true)
            {
                // 检查组件和Session是否仍然有效
                if (self.InstanceId != instanceId || self.IsDisposed || session.IsDisposed)
                {
                    return;
                }

                long timeNow = TimeInfo.Instance.ClientFrameTime();
                long timeSinceLastRecv = timeNow - session.LastRecvTime;
                
                // 如果最近有收到消息，使用长间隔检查，减少CPU开销
                if (timeSinceLastRecv < NO_MESSAGE_TIMEOUT)
                {
                    totalFailures = 0;
                    await timerComponent.WaitAsync(NORMAL_CHECK_INTERVAL);
                    continue;
                }
                
                // 超过7秒无消息，可能有问题，使用短间隔检查
                await timerComponent.WaitAsync(PROBLEM_CHECK_INTERVAL);
                
                // 再次检查有效性
                if (self.InstanceId != instanceId || self.IsDisposed || session.IsDisposed)
                {
                    return;
                }
                
                try
                {
                    (uint localConn, uint remoteConn) = session.AService.GetChannelConn(session.Id);
                    
                    // 验证连接ID有效性（任一为0都无效）
                    if (localConn == 0 || remoteConn == 0)
                    {
                        totalFailures++;
                        if (totalFailures >= maxTotalFailures)
                        {
                            Log.Error($"路由重连失败{maxTotalFailures}次，断开连接");
                            session.Error = ErrorCore.ERR_KcpConnectTimeout;
                            session.Dispose();
                            return;
                        }
                        continue;
                    }

                    (uint recvLocalConn, IPEndPoint routerAddress) = await netComponent.GetRouterAddress(realAddress, localConn, remoteConn);
                    if (recvLocalConn == 0)
                    {
                        totalFailures++;
                        if (totalFailures >= maxTotalFailures)
                        {
                            Log.Error($"路由重连失败{maxTotalFailures}次，断开连接");
                            session.Error = ErrorCore.ERR_KcpConnectTimeout;
                            session.Dispose();
                            return;
                        }
                        continue;
                    }
                    
                    // 重连成功，更新Session地址
                    session.AService.ChangeAddress(session.Id, routerAddress);
                    totalFailures = 0;
                    
                    // 等待连接稳定，不手动更新LastRecvTime，等待实际收到消息
                    await timerComponent.WaitAsync(1000);
                }
                catch (Exception e)
                {
                    totalFailures++;
                    if (totalFailures >= maxTotalFailures)
                    {
                        Log.Error($"路由重连异常{maxTotalFailures}次，断开连接: {e}");
                        session.Error = ErrorCore.ERR_KcpConnectTimeout;
                        session.Dispose();
                        return;
                    }
                    
                    if (session.IsDisposed)
                    {
                        return;
                    }
                }
            }
        }
    }
}