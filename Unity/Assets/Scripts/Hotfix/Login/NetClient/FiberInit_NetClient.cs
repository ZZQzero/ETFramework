namespace ET
{
    [Invoke(SceneType.NetClient)]
    public class FiberInit_NetClient: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            Log.Error($"FiberInit_NetClient {root.Name}  {root.Fiber.Id}");
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<FiberParentComponent>();
            var routerAddressComponent = root.AddComponent<RouterAddressComponent,string>(GlobalConfigManager.Instance.Config.IPAddress);
#if UNITY_WEBGL
            root.AddComponent<NetComponent, IKcpTransport>(new WebSocketTransport(routerAddressComponent.AddressFamily));
#else
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(routerAddressComponent.AddressFamily));
#endif
            await ETTask.CompletedTask;
        }
    }
}