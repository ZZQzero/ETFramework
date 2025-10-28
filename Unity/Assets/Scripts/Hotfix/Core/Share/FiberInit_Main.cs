
namespace ET
{
    public class FiberInit_Main: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            int sceneType = SceneTypeSingleton.Instance.GetSceneType(Options.Instance.SceneName);
            root.SceneType = sceneType;
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
#if UNITY
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
#elif DOTNET
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
#endif
        }
    }
}