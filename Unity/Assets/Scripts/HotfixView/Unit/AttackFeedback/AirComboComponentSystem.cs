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

                // 以攻击者为连段中心（XZ）
                self.ComboCenterWorldPos = attackerWorldPos;

                // 水平距离上限：优先使用本次命中的 HitBox 半径，否则用 profile
                var distance = attackRadiusOverride > 0f ? attackRadiusOverride : comboProfile.MaxAirHorizontalDistance;
                self.MaxHorizontalDistance = distance * comboProfile.MaxAirHorizontalScale;
                self.MaxHorizontalSpeed    = comboProfile.MaxAirHorizontalSpeed;
                self.RecenterStrength      = comboProfile.RecenterStrength;
                self.RecenterDeadZone      = comboProfile.RecenterDeadZone;
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
            
            var distance = attackRadiusOverride > 0f ? attackRadiusOverride : comboProfile.MaxAirHorizontalDistance;
            self.MaxHorizontalDistance = distance * comboProfile.MaxAirHorizontalScale;
            
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

        /// <summary>
        /// 横向速度上限
        /// </summary>
        /// <param name="self"></param>
        /// <param name="horizontalVelocity"></param>
        public static void ApplyAirComboHorizontalSpeedClamp(this AirComboComponent self, ref Vector3 horizontalVelocity)
        {
            if (!self.Active || self.IsExiting)
                return;

            float maxSpeed = self.MaxHorizontalSpeed;
            if (maxSpeed <= 0f)
                return;

            float speed = horizontalVelocity.magnitude;
            if (speed > maxSpeed)
            {
                horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            }
        }
        
        /// <summary>
        /// 回拉机制：当怪物超出连段中心的最大水平距离时，施加朝向中心的回拉速度，防止怪物被打出攻击范围。
        /// 回拉速度与超出距离成正比，直接叠加到目标速度（单位 m/s）。
        /// </summary>
        public static void ApplyAirComboHorizontalRecenter(this AirComboComponent self, Vector3 currentWorldPos, ref Vector3 horizontalVelocity)
        {
            if (!self.Active || self.IsExiting)
                return;

            Vector3 center = self.ComboCenterWorldPos;

            Vector3 offset = currentWorldPos - center;
            offset.y = 0f;

            float dist = offset.magnitude;
            if (dist <= self.RecenterDeadZone)
                return;

            float maxDist = self.MaxHorizontalDistance;
            if (maxDist <= 0f)
                return;

            float excess = dist - maxDist;
            if (excess <= 0f)
                return;

            Vector3 dirToCenter = -offset.normalized;

            // 回拉速度与超出距离成正比（单位 m/s，直接叠加到目标速度，不乘 deltaTime）
            float pullSpeed = excess * self.RecenterStrength;

            horizontalVelocity += dirToCenter * pullSpeed;
        }


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

            self.ComboCenterWorldPos = Vector3.zero;
            self.MaxHorizontalDistance = 0f;
            self.MaxHorizontalSpeed = 0f;
            self.RecenterStrength = 0f;
            self.RecenterDeadZone = 0f;

            self.OnExitCompleted.Invoke();
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

