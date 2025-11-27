namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class A2C_DisconnectHandler : MessageHandler<Scene, A2C_Disconnect>
    {
        protected override async ETTask Run(Scene scene, A2C_Disconnect message)
        {
            Log.Error("A2C_Disconnect");
            await ETTask.CompletedTask;
        }
    }
}