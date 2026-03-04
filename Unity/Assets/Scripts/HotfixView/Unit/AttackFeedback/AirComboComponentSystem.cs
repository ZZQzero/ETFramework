using System;
using UnityEngine;

namespace ET
{
    public static partial class AirComboComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AirComboComponent self)
        {
            // 数据组件默认值兜底（避免未初始化导致的 NaN/0 陷阱）
            self.Active = false;
            self.IsExiting = false;
            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;
            self.ExitLerpMs = Mathf.Max(0, self.ExitLerpMs);
            self.MinFallSpeed = self.MinFallSpeed == 0f ? -1f : self.MinFallSpeed;
        }
        
        public static void Enter(
            this AirComboComponent self, 
            long hitStunEndTimeMs, 
            Vector3 attackerWorldPos, 
            in HitAirComboProfile comboProfile,
            float attackRadiusOverride = 0f)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!comboProfile.Enable)
            {
                return;
            }

            bool wasActive = self.Active;
            self.Active = true;
            // 进入/再次进入：取消退出（命中续期不应被 ExitLerp 打断）
            self.IsExiting = false;
            self.ExitStartCombatMs = 0;
            
            // KeepAlive：攻击段超时 + MaxAirTimeMs 偏移，确保空连维持到下一段命中
            self.AirEndCombatMs = hitStunEndTimeMs + comboProfile.MaxAirOffsetMs;
            
            // 合并参数（保守）
            float g = Mathf.Clamp01(comboProfile.GravityScaleDuringCombo);
            self.GravityScaleTarget = wasActive ? Mathf.Min(self.GravityScaleTarget, g) : g;

            self.ExitLerpMs = wasActive ? Mathf.Max(self.ExitLerpMs, comboProfile.ExitLerpMs) : Mathf.Max(0, comboProfile.ExitLerpMs);
           
            // 下落速度下限：abs 越小越”挂住”（最终 MinFallSpeed 越接近 0）
            float minFallAbs = Mathf.Max(0f, comboProfile.MinFallSpeedAbs);
            float nextMinFall = -minFallAbs;
            self.MinFallSpeed = wasActive ? Mathf.Max(self.MinFallSpeed, nextMinFall) : nextMinFall;
        }
        
        public static void OnHit(
            this AirComboComponent self, 
            long hitStunEndTimeMs, 
            in HitAirComboProfile comboProfile,
            float attackRadiusOverride = 0f)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!self.Active)
            {
                return;
            }

            // 命中续期：取消退出，保持挂空中
            if (self.IsExiting)
            {
                self.IsExiting = false;
                self.ExitStartCombatMs = 0;
            }
            
            // 注：水平距离约束已迁移到 HitTether 统一管理
            
            // KeepAlive：攻击段超时 + MaxAirTimeMs 偏移，确保空连维持到下一段命中
            self.AirEndCombatMs = hitStunEndTimeMs + comboProfile.MaxAirOffsetMs;
            
            // 参数合并（同 Enter 的保守策略）
            float g = Mathf.Clamp01(comboProfile.GravityScaleDuringCombo);
            self.GravityScaleTarget = Mathf.Min(self.GravityScaleTarget, g);

            self.ExitLerpMs = Mathf.Max(self.ExitLerpMs, comboProfile.ExitLerpMs);

            float minFallAbs = Mathf.Max(0f, comboProfile.MinFallSpeedAbs);
            self.MinFallSpeed = Mathf.Max(self.MinFallSpeed, -minFallAbs);
        }

        // 注：水平距离约束已迁移到 HitReactionComponentSystem.UpdateNormalHitMotion
        // 弹簧模型统一管理（NormalHit 地面+空中共用）。

        public static void BeginExit(this AirComboComponent self, long nowCombatMs)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!self.Active)
            {
                return;
            }

            if (self.IsExiting)
            {
                return;
            }

            self.IsExiting = true;
            self.ExitStartCombatMs = nowCombatMs;
            self.ExitFromGravityScale = self.GravityScaleTarget;
        }

        public static void ForceEnd(this AirComboComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            // 已经结束，直接返回（防止与 OnLand 竞态导致重复清理）
            if (!self.Active)
            {
                return;
            }

            self.Active = false;
            self.IsExiting = false;

            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;

            self.MinFallSpeed = -1f;

            // 注：水平距离字段（ComboCenterWorldPos 等）已迁移到 HitTether 统一管理
        }

        public static float GetCurrentGravityScale(this AirComboComponent self, long nowCombatMs)
        {
            if (self == null || self.IsDisposed || !self.Active)
            {
                return 1f;
            }

            if (!self.IsExiting)
            {
                return self.GravityScaleTarget;
            }

            int lerpMs = Mathf.Max(1, self.ExitLerpMs);
            float t = Mathf.Clamp01((float)(nowCombatMs - self.ExitStartCombatMs) / lerpMs);
            return Mathf.Lerp(self.ExitFromGravityScale, 1f, t);
        }

        public static bool IsExitCompleted(this AirComboComponent self, long nowCombatMs)
        {
            if (self == null || self.IsDisposed || !self.Active || !self.IsExiting)
            {
                return false;
            }
            return self.ExitLerpMs <= 0 || nowCombatMs - self.ExitStartCombatMs >= self.ExitLerpMs;
        }
        
    }
}

