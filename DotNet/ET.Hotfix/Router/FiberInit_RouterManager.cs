using System.Net;

namespace ET
{
    [Invoke(SceneType.RouterManager)]
    public class FiberInit_RouterManager: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            root.AddComponent<TimerComponent>();
            StartSceneTable startSceneConfig = StartSceneConfig.Instance.Get((int)root.Id);
            var ip = $"http://*:{startSceneConfig.Port}/";
            root.AddComponent<HttpComponent, string>(ip);
            await ETTask.CompletedTask;
        }
    }
}