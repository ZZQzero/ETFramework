namespace ET;

[MessageHandler(SceneType.Map)]
public class G2M_RequestExitGameHandler : MessageLocationHandler<Unit,G2M_RequestExitGame,M2G_RequestExitGame>
{
    protected override async ETTask Run(Unit unit, G2M_RequestExitGame request, M2G_RequestExitGame response)
    {
        await RemoveUnit(unit);
    }

    private async ETTask RemoveUnit(Unit unit)
    {
        if (unit == null || unit.IsDisposed)
        {
            return;
        }
          
        // TODO: 保存Unit数据到数据库
        // await SaveUnitData(unit);
          
        // 等待当前帧结束，确保所有消息处理完成
        await unit.Fiber().WaitFrameFinish();
        // 重新创建Unit时，GateSession可能已经换了，需要清理消息缓存，保证重新从Location拉取ActorId
        var locationSenderComponent = unit.Root().GetComponent<MessageLocationSenderComponent>();
        locationSenderComponent.Get(LocationType.GateSession).Remove(unit.Id);
        // 移除Location注册
        await unit.RemoveLocation(LocationType.Unit);
        
        // 从UnitComponent移除Unit
        var unitComponent = unit.Root().GetComponent<UnitComponent>();
        if (unitComponent != null && !unitComponent.IsDisposed)
        {
            unitComponent.Remove(unit.Id);
        }
    }
}