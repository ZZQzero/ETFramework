namespace ET;

public class FiberInit_LoginCenter : AInvokeHandler<FiberInit, ETTask>
{
    public override async ETTask Handle(FiberInit fiberInit)
    {
        Log.Info($"{fiberInit.Fiber.Root}  |   {fiberInit.Fiber.Root.Name}   |  {fiberInit.Fiber.Root.SceneType}");
        await ETTask.CompletedTask;
    }
}