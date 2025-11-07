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
            
            // 失败计数策略
            const int maxTotalFailures = 3; // 总失败次数达到3次后断开连接
            int totalFailures = 0;
            
            // 检查是否达到失败上限并断开Session
            bool CheckAndDisconnectIfNeeded()
            {
                if (totalFailures >= maxTotalFailures)
                {
                    Log.Error($"路由重连总失败次数达到{maxTotalFailures}次，主动断开Session");
                    session.Error = ErrorCore.ERR_KcpConnectTimeout;
                    session.Dispose();
                    return true; // 已断开
                }
                return false; // 未断开
            }
            
            while (true)
            {
                if (self.InstanceId != instanceId || self.IsDisposed)
                {
                    return;
                }

                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(1000);
                
                if (self.InstanceId != instanceId || self.IsDisposed)
                {
                    return;
                }

                // 检查session是否仍然有效
                if (session.IsDisposed)
                {
                    Log.Warning("Session已释放，RouterCheck退出");
                    return;
                }

                long time = TimeInfo.Instance.ClientFrameTime();

                // 如果最近有收到消息，不需要重连
                if (time - session.LastRecvTime < 7 * 1000)
                {
                    totalFailures = 0; // 重置总失败计数
                    continue;
                }
                
                try
                {
                    long sessionId = session.Id;

                    // GetChannelConn可能抛出异常（channel不存在）
                    (uint localConn, uint remoteConn) = session.AService.GetChannelConn(sessionId);
                    
                    // 验证连接ID有效性
                    if (localConn == 0 && remoteConn == 0)
                    {
                        Log.Warning($"连接ID无效: localConn={localConn}, remoteConn={remoteConn}");
                        totalFailures++;
                        
                        if (CheckAndDisconnectIfNeeded())
                        {
                            return;
                        }
                        continue;
                    }
                    
                    Log.Info($"get recvLocalConn start: {root.Id} {realAddress} {localConn} {remoteConn}");

                    (uint recvLocalConn, IPEndPoint routerAddress) = await netComponent.GetRouterAddress(realAddress, localConn, remoteConn);
                    if (recvLocalConn == 0)
                    {
                        Log.Error($"get recvLocalConn fail: {root.Id} {routerAddress} {realAddress} {localConn} {remoteConn}");
                        totalFailures++;
                        
                        if (CheckAndDisconnectIfNeeded())
                        {
                            return;
                        }
                        continue;
                    }
                    
                    Log.Info($"get recvLocalConn ok: {root.Id} {routerAddress} {realAddress} {recvLocalConn} {localConn} {remoteConn}");
                    
                    // 更新LastRecvTime，防止立即再次触发检查
                    session.LastRecvTime = TimeInfo.Instance.ClientNow();
                    
                    // ChangeAddress可能失败（channel不存在），但不会抛异常，只是静默返回
                    session.AService.ChangeAddress(sessionId, routerAddress);
                    
                    // 验证ChangeAddress是否成功（检查remoteAddress是否更新）
                    // 注意：这里不能立即检查，因为ChangeAddress可能是异步的
                    // 但我们可以通过下次检查时是否还有数据来判断
                    totalFailures = 0; // 成功，重置总失败计数
                    
                    // 切换地址后，等待一小段时间让连接稳定
                    await fiber.Root.GetComponent<TimerComponent>().WaitAsync(500);
                }
                catch (Exception e)
                {
                    totalFailures++;
                    Log.Error($"路由重连检查异常 (总失败{totalFailures}次): {e}");
                    
                    if (CheckAndDisconnectIfNeeded())
                    {
                        return;
                    }
                    
                    // 如果是session相关的异常，可能session已经断开，退出检查
                    if (session.IsDisposed)
                    {
                        Log.Warning("Session已断开，RouterCheck退出");
                        return;
                    }
                }
            }
        }
    }
}