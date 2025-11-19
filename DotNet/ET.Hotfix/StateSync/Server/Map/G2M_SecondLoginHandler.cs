namespace ET;

[MessageHandler(SceneType.Main)]
public class G2M_SecondLoginHandler : MessageLocationHandler<Unit,G2M_SecondLogin,M2G_SecondLogin>
{
    protected override async ETTask Run(Unit unit, G2M_SecondLogin request, M2G_SecondLogin response)
    {
        //TODO 二次登录！
        Log.Console("二次登录！");
        await ETTask.CompletedTask;
    }
}