namespace ET
{
    [MessageHandler(SceneType.Map)]
    public class G2M_SessionDisconnectHandler : MessageLocationHandler<Unit, G2M_SessionDisconnect>
    {
        protected override async ETTask Run(Unit unit, G2M_SessionDisconnect message)
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
          
            // 移除Location注册
            await unit.RemoveLocation(LocationType.Unit);
          
            // 从UnitComponent移除Unit
            var unitComponent = unit.Root().GetComponent<UnitComponent>();
            if (unitComponent != null && !unitComponent.IsDisposed)
            {
                unitComponent.Remove(unit.Id);
            }
          
            Log.Info($"Unit {unit.Id} 清理完成");
        }
    }
}