namespace ET
{
    [MessageSessionHandler(SceneType.NetClient)]
    public class A2C_DisconnectHandler : MessageSessionHandler<A2C_Disconnect>
    {
        protected override async ETTask Run(Session session, A2C_Disconnect message)
        {
            // 先保存 root 引用（Dispose 后无法访问）
            Scene root = session.Root();
            // 从 NetClient Fiber 向 Main Fiber 发送断线消息
            Fiber fiber = session.Fiber();
            NetClient2Main_SessionDispose disposeMessage = NetClient2Main_SessionDispose.Create();
            disposeMessage.Error = ErrorCode.ERR_KickedByServer; // 或其他错误码
            
            // 获取 Main Fiber 的 ActorId 并发送消息
            int mainFiberId = fiber.Root.GetComponent<FiberParentComponent>().ParentFiberId;
            fiber.Root.GetComponent<ProcessInnerSender>().Send(new ActorId(fiber.Process, mainFiberId), disposeMessage);
            
            // 断开 Session（Session 是 NetComponent 的子级，会自动被清理）
            session.Dispose();
            
            // 清理网络组件（下次登录时会重新添加）
            root.RemoveComponent<SessionComponent>();
            root.RemoveComponent<NetComponent>();
            
            await ETTask.CompletedTask;
        }
    }
}