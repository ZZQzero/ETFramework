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
            self.EnteredHeight = 0f;
            self.ComboMinHeight = 0f;
            self.ComboMaxHeight = 0f;
            self.HeightClampInitialized = false;
            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;
            self.ExitLerpMs = Mathf.Max(0, self.ExitLerpMs);
            self.MinFallSpeed = self.MinFallSpeed == 0f ? -1f : self.MinFallSpeed;
            self.MinHeightOffset = 0f;
            self.MaxHeightOffset = 0f;
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
            
            if (!wasActive)
            {
                self.EnteredHeight = attackerWorldPos.y;
                // 注：水平距离约束已迁移到 HitTether 统一管理
            }

            // KeepAlive：攻击段超时 + MaxAirTimeMs 偏移，确保空连维持到下一段命中
            self.AirEndCombatMs = hitStunEndTimeMs + comboProfile.MaxAirOffsetMs;
            
            // 合并参数（保守）
            float g = Mathf.Clamp01(comboProfile.GravityScaleDuringCombo);
            self.GravityScaleTarget = wasActive ? Mathf.Min(self.GravityScaleTarget, g) : g;

            self.ExitLerpMs = wasActive ? Mathf.Max(self.ExitLerpMs, comboProfile.ExitLerpMs) : Mathf.Max(0, comboProfile.ExitLerpMs);
           
            // 高度偏移合并：地板取更高、天花板取更低
            self.MinHeightOffset = wasActive ? Mathf.Max(self.MinHeightOffset, comboProfile.MinHeightOffset) : comboProfile.MinHeightOffset;
            self.MaxHeightOffset = wasActive ? Mathf.Min(self.MaxHeightOffset, comboProfile.MaxHeightOffset) : comboProfile.MaxHeightOffset;

            // 下落速度下限：abs 越小越“挂住”（最终 MinFallSpeed 越接近 0）
            float minFallAbs = Mathf.Max(0f, comboProfile.MinFallSpeedAbs);
            float nextMinFall = -minFallAbs;
            self.MinFallSpeed = wasActive ? Mathf.Max(self.MinFallSpeed, nextMinFall) : nextMinFall;

            self.RecalculateHeightClampFromEnteredHeight();
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

            self.MinHeightOffset = Mathf.Max(self.MinHeightOffset, comboProfile.MinHeightOffset);
            self.MaxHeightOffset = Mathf.Min(self.MaxHeightOffset, comboProfile.MaxHeightOffset);

            float minFallAbs = Mathf.Max(0f, comboProfile.MinFallSpeedAbs);
            self.MinFallSpeed = Mathf.Max(self.MinFallSpeed, -minFallAbs);

            self.RecalculateHeightClampFromEnteredHeight();
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
            // 会话结束必须清理“会话级别”的所有运行时参数：
            // - 否则下一段空中连段会继承上一段的 clamp/偏移/落地硬直等，出现越打越夹紧、悬空异常等问题

            self.EnteredHeight = 0f;
            self.ComboMinHeight = 0f;
            self.ComboMaxHeight = 0f;
            self.HeightClampInitialized = false;

            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;

            self.MinFallSpeed = -1f;
            self.MinHeightOffset = 0f;
            self.MaxHeightOffset = 0f;

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
        
        /// <summary>
        /// 根据当前 EnteredHeight 和 Offset 直接计算高度夹持区间（覆盖写入，不做增量合并）。
        /// offset 参数本身已在 Enter/OnHit 中按"地板取更高、天花板取更低"策略合并，
        /// 此处只需将最终 offset 转为绝对高度，避免 offset 合并 + clamp 合并双重收紧导致区间退化。
        /// </summary>
        private static void RecalculateHeightClampFromEnteredHeight(this AirComboComponent self)
        {
            float minY = self.EnteredHeight + self.MinHeightOffset;
            float maxY = self.EnteredHeight + self.MaxHeightOffset;

            if (maxY < minY)
            {
                (minY, maxY) = (maxY, minY);
            }

            self.ComboMinHeight = minY;
            self.ComboMaxHeight = maxY;
            self.HeightClampInitialized = true;
        }
    }
}

