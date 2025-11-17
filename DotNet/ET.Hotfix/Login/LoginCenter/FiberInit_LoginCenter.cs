namespace ET;

public class FiberInit_LoginCenter : AInvokeHandler<FiberInit, ETTask>
{
    public override async ETTask Handle(FiberInit fiberInit)
    {
        Log.Info($"{fiberInit.Fiber.Root}  |   {fiberInit.Fiber.Root.Name}   |  {fiberInit.Fiber.Root.SceneType}");
        var root = fiberInit.Fiber.Root;
        root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
        root.AddComponent<TimerComponent>();
        root.AddComponent<CoroutineLockComponent>();
        root.AddComponent<ProcessInnerSender>();
        root.AddComponent<MessageSender>();
        root.AddComponent<LoginInfoRecordComponent>();
        await ETTask.CompletedTask;
    }
}