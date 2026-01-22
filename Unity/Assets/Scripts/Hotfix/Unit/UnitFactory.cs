using Unity.Mathematics;

namespace ET
{
    public static partial class UnitFactory
    {
        public static Unit Create(Scene currentScene, UnitInfo unitInfo)
        {
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            var role = RoleConfig.Instance.Get(unitInfo.RoleConfigId);
            Unit unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.EntityId, role.UnitId);
            unit.AddComponent<RoleIdentityComponent, UnitInfo>(unitInfo);
            unit.Position = unitInfo.Position;
            unit.Forward = unitInfo.Forward;
	        
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();

            foreach (var kv in unitInfo.NumericDic)
            {
                numericComponent.Set(kv.Key, kv.Value);
            }
	        
            unit.AddComponent<MoveComponent>();
            if (unitInfo.MoveInfo != null)
            {
                if (unitInfo.MoveInfo.Points.Count > 0)
                {
                    unitInfo.MoveInfo.Points[0] = unit.Position;
                    unit.MoveToAsync(unitInfo.MoveInfo.Points).NoContext();
                }
            }

            unit.AddComponent<ObjectWait>();

            unit.AddComponent<XunLuoPathComponent>();
	        
            EventSystem.Instance.Publish(unit.Scene(), new AfterUnitCreate() {Unit = unit});
            return unit;
        }
    }
}