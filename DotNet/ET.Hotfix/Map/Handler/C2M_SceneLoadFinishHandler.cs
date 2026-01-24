namespace ET
{
    /// <summary>
    /// 处理客户端场景加载完成通知
    /// 客户端Unity场景加载完成后调用此Handler，服务端收到后发送Unit创建消息
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_SceneLoadFinishHandler: MessageLocationHandler<Unit, C2M_SceneLoadFinish, M2C_SceneLoadFinish>
    {
        protected override async ETTask Run(Unit unit, C2M_SceneLoadFinish request, M2C_SceneLoadFinish response)
        {
            // 客户端场景加载完成，现在可以安全地发送Unit创建消息
            M2C_CreateMyUnit m2CCreateMyUnit = M2C_CreateMyUnit.Create();
            m2CCreateMyUnit.Unit = UnitHelper.CreateUnitInfo(unit);
            Log.Info($"{m2CCreateMyUnit.Unit.RoleConfigId}  {m2CCreateMyUnit.Unit.EntityId}  {m2CCreateMyUnit.Unit.RoleId}");
            MapMessageHelper.SendToClient(unit, m2CCreateMyUnit);
			

            M2C_CreateUnits otherUnits = M2C_CreateUnits.Create();
            UnitComponent unitComponent = unit.GetParent<UnitComponent>();
            
            foreach (var otherUnit in unitComponent.Children)
            {
                var monsterUnit =  (Unit)otherUnit.Value;
                if (monsterUnit.UnitType() != UnitType.Monster)
                {
                    continue;
                }

                otherUnits.Units.Add(UnitHelper.CreateUnitInfo(monsterUnit));
            }
            MapMessageHelper.SendToClient(unit, otherUnits);
			
            Log.Info($"Unit场景加载完成: {unit.EntityId}, SceneName: {request.SceneName}");
			
            await ETTask.CompletedTask;
        }
    }
}