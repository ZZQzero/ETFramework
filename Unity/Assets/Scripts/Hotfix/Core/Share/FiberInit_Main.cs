
namespace ET
{
    [Invoke(SceneType.Main)]
    public class FiberInit_Main: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
#if UNITY
            root.AddComponent<ClientSenderComponent,Fiber>(fiberInit.Fiber);
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
#elif DOTNET
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
#endif
        }
    }
}