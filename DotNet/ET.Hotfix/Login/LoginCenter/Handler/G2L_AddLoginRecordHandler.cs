namespace ET;

[MessageHandler(SceneType.LoginCenter)]
public class G2L_AddLoginRecordHandler : MessageHandler<Scene,G2L_AddLoginRecord,L2G_AddLoginRecord>
{
    protected override async ETTask Run(Scene scene, G2L_AddLoginRecord request, L2G_AddLoginRecord response)
    {
        var loginInfo = scene.GetComponent<LoginInfoRecordComponent>();
        loginInfo.Remove(request.UserId);
        loginInfo.Add(request.UserId,request.ServerId);
        await ETTask.CompletedTask;
    }
}