using Unity.Mathematics;

namespace ET
{
    public static partial class UnitFactory
    {
        public static Unit Create(Scene currentScene, UnitInfo unitInfo)
        {
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = null;
            switch (unitInfo.UnitType)
            {
                case UnitType.Player:
                    var role = RoleConfig.Instance.Get(unitInfo.RoleConfigId);
                    unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.EntityId, role.UnitId);
                    unit.AddComponent<RoleIdentityComponent, UnitInfo>(unitInfo);
                    break;
                case UnitType.Monster:
                    var monster = MonsterConfig.Instance.Get(unitInfo.MonsterConfigId);
                    unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.EntityId, monster.UnitId);
                    unit.AddComponent<MonsterIdentityComponent, MonsterTable>(monster);
                    // 运动系统重构：怪物移动由 AI Driver → Intent → Motor 负责。
                    // 旧的 MoveComponent/寻路同步链路会直接写 Unit.Position，容易与 Rigidbody Motor 冲突产生抖动/穿模。
                    break;
                case UnitType.NPC:
                    break;
            }
          
            unit.Position = unitInfo.Position;
            unit.Forward = unitInfo.Forward;
	        
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();

            foreach (var kv in unitInfo.NumericDic)
            {
                numericComponent.Set(kv.Key, kv.Value);
            }
            unit.AddComponent<ObjectWait>();

	        
            EventSystem.Instance.Publish(unit.Scene(), new AfterUnitCreate() {Unit = unit});
            return unit;
        }
    }
}