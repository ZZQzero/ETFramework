using System;
using Unity.Mathematics;

namespace ET
{
    public static partial class UnitFactory
    {
        public static Unit CreatePlayer(Scene scene,UnitInfo info)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            RoleTable roleTable = RoleConfig.Instance.Get(info.RoleConfigId);
            Unit unit = unitComponent.AddChildWithId<Unit, int>(info.EntityId,roleTable.UnitId);
            unit.AddComponent<RoleIdentityComponent, UnitInfo>(info);
            unit.AddComponent<MoveComponent>();
            unit.Position = new float3(-10, 0, -10);
            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            numericComponent.Set(NumericType.Speed, 6f); // 速度是6米每秒
            numericComponent.Set(NumericType.AOI, 15000); // 视野15米

            // 加入aoi
            unit.AddComponent<AOIEntity, int, float3>(9 * 1000, unit.Position);
            return unit;
        }

        /// <summary>
        /// 通过 MonsterConfig 创建怪物 Unit（注意：MonsterTable.UnitId 必须指向 Unit表 中 Type=UnitType.Monster(5002) 的那一行）
        /// </summary>
        public static Unit CreateMonster(Scene scene, MonsterTable monsterTable, float3 position)
        {
            // 生成一个全局唯一的 UnitId（long）
            long entity = GenerateIdManager.Instance.GenerateId();

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            Unit unit = unitComponent.AddChildWithId<Unit, int>(entity, monsterTable.UnitId);
            unit.AddComponent<MonsterIdentityComponent,MonsterTable>(monsterTable);
            // 基础组件：至少要有 Numeric + AOIEntity，否则 AOI 同步 UnitInfo 时会空指针
            unit.AddComponent<MoveComponent>();
            unit.Position = position;

            NumericComponent nc = unit.AddComponent<NumericComponent>();
            nc.Set(NumericType.Speed, monsterTable.Speed);
            nc.Set(NumericType.AOI, monsterTable.AOI);
            nc.Set(NumericType.MaxHp, monsterTable.HP);
            nc.Set(NumericType.Hp, monsterTable.HP);

            // 加入 AOI（视野距离建议直接用配置）
            unit.AddComponent<AOIEntity, int, float3>(monsterTable.AOI, unit.Position);

            // 基础寻路（可选，但通常怪物 AI 会用到）
            unit.AddComponent<PathfindingComponent, string>(scene.Name);

            // 安全检查：如果 Unit表 配错 Type，这里会导致怪物被当成 Player/NPC 等
            if (unit.UnitType() != UnitType.Monster)
            {
                Log.Error($"MonsterConfig.UnitId 对应的 Unit表.Type 不为 Monster: monsterConfigId={monsterTable.Id}, unitConfigId={monsterTable.UnitId}, unitType={unit.UnitType()}");
            }

            return unit;
        }
    }
}