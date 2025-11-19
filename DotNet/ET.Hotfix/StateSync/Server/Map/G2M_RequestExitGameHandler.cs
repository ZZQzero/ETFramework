namespace ET;

[MessageHandler(SceneType.Map)]
public class G2M_RequestExitGameHandler : MessageLocationHandler<Unit,G2M_RequestExitGame,M2G_RequestExitGame>
{
    protected override async ETTask Run(Unit unit, G2M_RequestExitGame request, M2G_RequestExitGame response)
    {
        //TODO unit角色下线逻辑，保存数据到数据库
        RemoveUnit(unit).NoContext();
        await ETTask.CompletedTask;
    }

    private async ETTask RemoveUnit(Unit unit)
    {
        await unit.Fiber().WaitFrameFinish();
        await unit.RemoveLocation(LocationType.Unit);
        var unitComponent = unit.Root().GetComponent<UnitComponent>();
        unitComponent.Remove(unit.Id);
    }
}