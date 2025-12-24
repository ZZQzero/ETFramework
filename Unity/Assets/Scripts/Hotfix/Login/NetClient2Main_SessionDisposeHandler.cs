namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class NetClient2Main_SessionDisposeHandler: MessageHandler<Scene, NetClient2Main_SessionDispose>
    {
        protected override async ETTask Run(Scene root, NetClient2Main_SessionDispose message)
        {
            // 在 Main Fiber 中移除 UnitComponent（清理所有单位）
            UnitComponent unitComponent = root.GetComponent<UnitComponent>();
            if (unitComponent != null)
            {
                unitComponent.Dispose();
            }
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
            // 可以发布断线事件，通知其他模块
            //await EventSystem.Instance.PublishAsync(root, new SessionDispose() { Error = message.Error });
            
            await ETTask.CompletedTask;
        }
    }
}