namespace ET
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_DisconnectHandler : MessageHandler<Scene,Main2NetClient_Disconnect>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Disconnect message)
        {
            NetClient2Main_SessionDispose disposeMessage = NetClient2Main_SessionDispose.Create();
            int mainFiberId = root.GetComponent<FiberParentComponent>().ParentFiberId;
            root.GetComponent<ProcessInnerSender>().Send(new ActorId(root.Fiber.Process, mainFiberId), disposeMessage);
            root.RemoveComponent<SessionComponent>();
            root.RemoveComponent<NetComponent>();
            await ETTask.CompletedTask;
        }
    }
}