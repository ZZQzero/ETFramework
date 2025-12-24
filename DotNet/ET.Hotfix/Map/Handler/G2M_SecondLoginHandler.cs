namespace ET;

[MessageHandler(SceneType.Map)]
public class G2M_SecondLoginHandler : MessageLocationHandler<Unit,G2M_SecondLogin,M2G_SecondLogin>
{
    protected override async ETTask Run(Unit unit, G2M_SecondLogin request, M2G_SecondLogin response)
    {
        // 二次登录：客户端重新连接，需要更新GateSession的绑定
        // 1. 清理旧的GateSession Location缓存，确保Map服务器重新从Location拉取新的ActorId
        var locationSenderComponent = unit.Root().GetComponent<MessageLocationSenderComponent>();
        locationSenderComponent.Get(LocationType.GateSession).Remove(unit.Id);
        response.Error = ErrorCode.ERR_Success;
        
        // 2. 等待Gate服务器更新完Location（通过RPC完成时间保证时序）
        // Gate服务器会在更新完Location后才返回RPC响应
        // 此时可以安全地发送消息，Location已经指向新的GateSession
        
        // 3. 发送场景切换和创建Unit消息（保持与传送流程一致）
        Scene unitScene = unit.Scene();
        M2C_StartSceneChange m2CStartSceneChange = M2C_StartSceneChange.Create();
        m2CStartSceneChange.SceneInstanceId = unitScene.InstanceId;
        m2CStartSceneChange.SceneName = unitScene.Name;
        MapMessageHelper.SendToClient(unit, m2CStartSceneChange);

        M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
        m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(unit);
        MapMessageHelper.SendToClient(unit, m2CCreateMyUnit);
        await ETTask.CompletedTask;
    }
}