
namespace ET
{
    [Invoke(SceneType.Main)]
    public class FiberInit_Main: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            
            int sceneType = SceneTypeSingleton.Instance.GetSceneType(Options.Instance.SceneName);
            root.SceneType = sceneType;
            Log.Info($"FiberInit_Main {root.SceneType}  {root.Name}");
            await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
            await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
        }
    }
}