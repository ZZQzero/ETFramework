namespace ET
{
    // 离开视野
    [Event(SceneType.Map)]
    public class UnitLeaveSightRange_NotifyClient: AEvent<Scene, UnitLeaveSightRange>
    {
        protected override async ETTask Run(Scene scene, UnitLeaveSightRange args)
        {
            await ETTask.CompletedTask;
            AOIEntity a = args.A;
            AOIEntity b = args.B;
            if (a.Unit.Type() != UnitType.Player)
            {
                return;
            }

            var unit = a.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                Log.Error("unit is null or disposed");
                return;
            }
            MapMessageHelper.NoticeUnitRemove(a.GetParent<Unit>(), b.GetParent<Unit>());
        }
    }
}