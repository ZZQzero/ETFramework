using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(AOIEntity))]
    [FriendOf(typeof(Cell))]
    public static partial class AOIEntitySystem2
    {
        [EntitySystem]
        private static void Awake(this AOIEntity self, int distance, float3 pos)
        {
            self.ViewDistance = distance;
            self.Scene().GetComponent<AOIManagerComponent>().Add(self, pos.x, pos.z);
        }

        [EntitySystem]
        private static void Destroy(this AOIEntity self)
        {
            self.Scene().GetComponent<AOIManagerComponent>()?.Remove(self);
            self.ViewDistance = 0;
            self.SeeUnits.Clear();
            self.SeePlayers.Clear();
            self.BeSeePlayers.Clear();
            self.BeSeeUnits.Clear();
            self.SubEnterCells.Clear();
            self.SubLeaveCells.Clear();
        }
    }

    [FriendOf(typeof(Cell))]
    [FriendOf(typeof(AOIEntity))]
    public static partial class AOIEntitySystem
    {
        // 获取在自己视野中的对象
        public static Dictionary<long, EntityRef<AOIEntity>> GetSeeUnits(this AOIEntity self)
        {
            return self.SeeUnits;
        }

        public static Dictionary<long, EntityRef<AOIEntity>> GetBeSeePlayers(this AOIEntity self)
        {
            return self.BeSeePlayers;
        }

        public static Dictionary<long, EntityRef<AOIEntity>> GetSeePlayers(this AOIEntity self)
        {
            return self.SeePlayers;
        }

        // cell中的unit进入self的视野
        public static void SubEnter(this AOIEntity self, Cell cell)
        {
            cell.SubsEnterEntities.Add(self.EntityId, self);
            foreach (KeyValuePair<long, EntityRef<AOIEntity>> kv in cell.AOIUnits)
            {
                if (kv.Key == self.EntityId)
                {
                    continue;
                }

                self.EnterSight(kv.Value);
            }
        }

        public static void UnSubEnter(this AOIEntity self, Cell cell)
        {
            cell.SubsEnterEntities.Remove(self.EntityId);
        }

        public static void SubLeave(this AOIEntity self, Cell cell)
        {
            cell.SubsLeaveEntities.Add(self.EntityId, self);
        }

        // cell中的unit离开self的视野
        public static void UnSubLeave(this AOIEntity self, Cell cell)
        {
            foreach (KeyValuePair<long, EntityRef<AOIEntity>> kv in cell.AOIUnits)
            {
                if (kv.Key == self.EntityId)
                {
                    continue;
                }

                self.LeaveSight(kv.Value);
            }

            cell.SubsLeaveEntities.Remove(self.EntityId);
        }

        // enter进入self视野
        public static void EnterSight(this AOIEntity self, AOIEntity enter)
        {
            // 检查enter是否已被销毁
            if (enter == null || enter.IsDisposed)
            {
                return;
            }
            
            // 有可能之前在Enter，后来出了Enter还在LeaveCell，这样仍然没有删除，继续进来Enter，这种情况不需要处理
            if (self.SeeUnits.ContainsKey(enter.EntityId))
            {
                return;
            }
            
            if (!AOISeeCheckHelper.IsCanSee(self, enter))
            {
                return;
            }

            if (self.Unit.Type() == UnitType.Player)
            {
                if (enter.Unit.Type() == UnitType.Player)
                {
                    self.SeeUnits.Add(enter.EntityId, enter);
                    enter.BeSeeUnits.Add(self.EntityId, self);
                    self.SeePlayers.Add(enter.EntityId, enter);
                    enter.BeSeePlayers.Add(self.EntityId, self);
                    
                }
                else
                {
                    self.SeeUnits.Add(enter.EntityId, enter);
                    enter.BeSeeUnits.Add(self.EntityId, self);
                    enter.BeSeePlayers.Add(self.EntityId, self);
                }
            }
            else
            {
                if (enter.Unit.Type() == UnitType.Player)
                {
                    self.SeeUnits.Add(enter.EntityId, enter);
                    enter.BeSeeUnits.Add(self.EntityId, self);
                    self.SeePlayers.Add(enter.EntityId, enter);
                }
                else
                {
                    self.SeeUnits.Add(enter.EntityId, enter);
                    enter.BeSeeUnits.Add(self.EntityId, self);
                }
            }
            EventSystem.Instance.Publish(self.Scene(), new UnitEnterSightRange() { A = self, B = enter });
        }

        // leave离开self视野
        public static void LeaveSight(this AOIEntity self, AOIEntity leave)
        {
            // 检查leave是否已被销毁
            if (leave == null || leave.IsDisposed)
            {
                return;
            }
            
            if (self.EntityId == leave.EntityId)
            {
                return;
            }

            if (!self.SeeUnits.ContainsKey(leave.EntityId))
            {
                return;
            }

            self.SeeUnits.Remove(leave.EntityId);
            if (leave.Unit.Type() == UnitType.Player)
            {
                self.SeePlayers.Remove(leave.EntityId);
            }

            leave.BeSeeUnits.Remove(self.EntityId);
            if (self.Unit.Type() == UnitType.Player)
            {
                leave.BeSeePlayers.Remove(self.EntityId);
            }

            EventSystem.Instance.Publish(self.Scene(), new UnitLeaveSightRange { A = self, B = leave });
        }

        /// <summary>
        /// 是否在Unit视野范围内
        /// </summary>
        /// <param name="self"></param>
        /// <param name="unitId"></param>
        /// <returns></returns>
        public static bool IsBeSee(this AOIEntity self, long unitId)
        {
            return self.BeSeePlayers.ContainsKey(unitId);
        }
    }
}