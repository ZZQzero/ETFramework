using System;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(AirComboComponent))]
    [FriendOf(typeof(AirComboComponent))]
    public static partial class AirComboComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AirComboComponent self)
        {
            // 数据组件默认值兜底（避免未初始化导致的 NaN/0 陷阱）
            self.Active = false;
            self.IsExiting = false;
            self.EndCombatMs = 0;
            self.AbsoluteEndCombatMs = 0;
            self.PendingCaptureEnteredHeight = false;
            self.EnteredHeight = 0f;
            self.ComboMinHeight = 0f;
            self.ComboMaxHeight = 0f;
            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;
            self.ExitLerpMs = Mathf.Max(0, self.ExitLerpMs);
            self.MinFallSpeed = self.MinFallSpeed == 0f ? -1f : self.MinFallSpeed;
            self.MinHeightOffset = 0f;
            self.MaxHeightOffset = 0f;
            self.LandingStunMs = 0;
            self.DebugLogIntervalMs = Mathf.Max(0, self.DebugLogIntervalMs);
            self.LastDebugLogCombatMs = 0;
            self.ForceEndGroundedFramesThreshold = Mathf.Max(1, self.ForceEndGroundedFramesThreshold);
        }

        /// <param name="minAirTimeMsOverride">若 > 0，与 profile.MinAirTimeMs 取 max，用于与攻击方段超时(GetCurrentSegmentComboTimeoutMs)对齐。</param>
        /// <param name="attackRadiusOverride">若 > 0，用 HitBox.Size 推导的半径作为水平距离上限（与 PhysicsHelper 判定一致）；0 表示使用 profile.MaxAirHorizontalDistance。</param>
        public static void Enter(this AirComboComponent self, long nowCombatMs, Vector3 attackerWorldPos, in HitAirComboProfile profile, int minAirTimeMsOverride = 0,int totalAirTimeMs = 0, float attackRadiusOverride = 0f)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!profile.Enable)
            {
                return;
            }

            bool wasActive = self.Active;
            self.Active = true;
            // 进入/再次进入：取消退出（命中续期不应被 ExitLerp 打断）
            self.IsExiting = false;
            self.ExitStartCombatMs = 0;

            // EnteredHeight：首次进入开启延迟捕获；再次进入不重置锚点，避免高度漂移导致 clamp 抖动
            if (!wasActive)
            {
                self.PendingCaptureEnteredHeight = true;
                // fallback：先记录当前高度（避免极端情况下永远不捕获）
                self.EnteredHeight = attackerWorldPos.y;
                
                // 进入空中连段：请求关闭地检（由 HitReaction 统一处理）
                self.OnGroundDetectRequested.Invoke(false);

                // 以攻击者为连段中心（XZ）
                self.ComboCenterWorldPos = attackerWorldPos;

                // 水平距离上限：优先使用本次命中的 HitBox 半径，否则用 profile
                var distance = attackRadiusOverride > 0f ? attackRadiusOverride : profile.MaxAirHorizontalDistance;
                self.MaxHorizontalDistance = distance * profile.MaxAirHorizontalScale;
                self.MaxHorizontalSpeed    = profile.MaxAirHorizontalSpeed;
                self.RecenterStrength      = profile.RecenterStrength;
                self.RecenterDeadZone      = profile.RecenterDeadZone;
            }

            // KeepAlive（至少维持一段时间）；可与攻击方段超时对齐，避免“攻击动画未结束就 BeginExit”
            int minAirMs = Mathf.Max(0, profile.MinAirTimeMs);
            int effectiveMinAirMs = minAirTimeMsOverride > 0 ? Mathf.Max(minAirMs, minAirTimeMsOverride) : minAirMs;
            long nextEnd = nowCombatMs + effectiveMinAirMs;
            if (!wasActive || nextEnd > self.EndCombatMs)
            {
                self.EndCombatMs = nextEnd;
            }

            // fail-safe：绝对上限只会变得更严格，不允许被不断延长
            // totalAirTimeMs：攻击方本次攻击的“总时长上限”（若提供则优先使用）；否则使用 profile.MaxTotalHangMs。
            // 注意：绝对上限必须 >= effectiveMinAirMs，否则会把 EndCombatMs 夹短，导致“攻击动画未结束就 BeginExit”。
            int totalMs = totalAirTimeMs > 0 ? totalAirTimeMs : profile.MaxTotalHangMs;
            totalMs = Mathf.Max(0, totalMs);
            int maxHangMs = Mathf.Max(effectiveMinAirMs, totalMs);
            
            long nextAbsEnd = nowCombatMs + maxHangMs;
            if (self.AbsoluteEndCombatMs <= 0)
            {
                self.AbsoluteEndCombatMs = nextAbsEnd;
            }
            else
            {
                self.AbsoluteEndCombatMs = Math.Min(self.AbsoluteEndCombatMs, nextAbsEnd);
            }
            if (self.EndCombatMs > self.AbsoluteEndCombatMs)
            {
                self.EndCombatMs = self.AbsoluteEndCombatMs;
            }

            // 合并参数（保守）
            float g = Mathf.Clamp01(profile.GravityScaleDuringCombo);
            self.GravityScaleTarget = wasActive ? Mathf.Min(self.GravityScaleTarget, g) : g;

            self.ExitLerpMs = wasActive ? Mathf.Max(self.ExitLerpMs, profile.ExitLerpMs) : Mathf.Max(0, profile.ExitLerpMs);
            self.LandingStunMs = wasActive ? Mathf.Max(self.LandingStunMs, profile.LandingStunMs) : Mathf.Max(0, profile.LandingStunMs);

            // 高度偏移合并：地板取更高、天花板取更低
            self.MinHeightOffset = wasActive ? Mathf.Max(self.MinHeightOffset, profile.MinHeightOffset) : profile.MinHeightOffset;
            self.MaxHeightOffset = wasActive ? Mathf.Min(self.MaxHeightOffset, profile.MaxHeightOffset) : profile.MaxHeightOffset;

            // 下落速度下限：abs 越小越“挂住”（最终 MinFallSpeed 越接近 0）
            float minFallAbs = Mathf.Max(0f, profile.MinFallSpeedAbs);
            float nextMinFall = -minFallAbs;
            self.MinFallSpeed = wasActive ? Mathf.Max(self.MinFallSpeed, nextMinFall) : nextMinFall;

            self.RecalculateHeightClampFromEnteredHeight();
            self.DebugLog(nowCombatMs, "Enter");
        }

        /// <param name="minAirTimeMsOverride">若 > 0，与 profile.MinAirTimeMs 取 max，用于与攻击方段超时对齐。</param>
        /// <param name="attackRadiusOverride">若 > 0，用本次 HitBox 半径收紧水平距离上限（取 min，保证不超出任意一次命中的攻击范围）。</param>
        public static void OnHit(this AirComboComponent self, long nowCombatMs, in HitAirComboProfile profile, int minAirTimeMsOverride = 0, float attackRadiusOverride = 0f)
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
            if (attackRadiusOverride > 0f)
            {
                self.MaxHorizontalDistance = Mathf.Min(self.MaxHorizontalDistance, attackRadiusOverride);
            }
            // KeepAlive：取更长的结束点，但不得超过绝对上限；可与攻击方段超时对齐
            int minAirMs = Mathf.Max(0, profile.MinAirTimeMs);
            int effectiveMinAirMs = minAirTimeMsOverride > 0 ? Mathf.Max(minAirMs, minAirTimeMsOverride) : minAirMs;
            long next = nowCombatMs + effectiveMinAirMs;
            if (next > self.EndCombatMs)
            {
                self.EndCombatMs = next;
            }
            if (self.EndCombatMs > self.AbsoluteEndCombatMs)
            {
                self.EndCombatMs = self.AbsoluteEndCombatMs;
            }

            // 参数合并（同 Enter 的保守策略）
            float g = Mathf.Clamp01(profile.GravityScaleDuringCombo);
            self.GravityScaleTarget = Mathf.Min(self.GravityScaleTarget, g);

            self.ExitLerpMs = Mathf.Max(self.ExitLerpMs, profile.ExitLerpMs);
            self.LandingStunMs = Mathf.Max(self.LandingStunMs, profile.LandingStunMs);

            self.MinHeightOffset = Mathf.Max(self.MinHeightOffset, profile.MinHeightOffset);
            self.MaxHeightOffset = Mathf.Min(self.MaxHeightOffset, profile.MaxHeightOffset);

            float minFallAbs = Mathf.Max(0f, profile.MinFallSpeedAbs);
            self.MinFallSpeed = Mathf.Max(self.MinFallSpeed, -minFallAbs);

            self.RecalculateHeightClampFromEnteredHeight();
            self.DebugLog(nowCombatMs, "OnHit");
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
        /// 回拉机制
        /// </summary>
        /// <param name="self"></param>
        /// <param name="currentWorldPos"></param>
        /// <param name="horizontalVelocity"></param>
        /// <param name="deltaTime"></param>
        public static void ApplyAirComboHorizontalRecenter(this AirComboComponent self, Vector3 currentWorldPos, ref Vector3 horizontalVelocity, float deltaTime)
        {
            if (!self.Active || self.IsExiting)
                return;

            Vector3 center = self.ComboCenterWorldPos;

            Vector3 offset = currentWorldPos - center;
            offset.y = 0f;

            float dist = offset.magnitude;
            if (dist <= self.RecenterDeadZone)
                return;

            float maxDist = self.MaxHorizontalDistance * 0.7f;
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

            // 进入退出阶段：请求开启地检
            self.OnGroundDetectRequested.Invoke(true);

            Log.Error("退出空中BeginExit");
            self.IsExiting = true;
            self.ExitStartCombatMs = nowCombatMs;
            self.ExitFromGravityScale = self.GravityScaleTarget;
            self.DebugLog(nowCombatMs, "BeginExit");
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
            self.EndCombatMs = 0;
            self.AbsoluteEndCombatMs = 0;

            self.PendingCaptureEnteredHeight = false;
            self.EnteredHeight = 0f;
            self.ComboMinHeight = 0f;
            self.ComboMaxHeight = 0f;

            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;

            self.MinFallSpeed = -1f;
            self.MinHeightOffset = 0f;
            self.MaxHeightOffset = 0f;
            self.LandingStunMs = 0;

            self.LastDebugLogCombatMs = 0;
            self.ComboCenterWorldPos = Vector3.zero;
            self.MaxHorizontalDistance = 0f;
            self.MaxHorizontalSpeed = 0f;
            self.RecenterStrength = 0f;
            self.RecenterDeadZone = 0f;

            self.OnExitCompleted.Invoke();
        }

        public static void CaptureEnteredHeightIfNeeded(this AirComboComponent self, float rigidbodyY)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!self.Active || !self.PendingCaptureEnteredHeight)
            {
                return;
            }

            self.PendingCaptureEnteredHeight = false;
            self.EnteredHeight = rigidbodyY;
            self.RecalculateHeightClampFromEnteredHeight();
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

        private static void DebugLog(this AirComboComponent self, long nowCombatMs, string reason)
        {
            if (!self.DebugEnabled)
            {
                return;
            }

            int interval = Mathf.Max(0, self.DebugLogIntervalMs);
            if (interval > 0 && nowCombatMs - self.LastDebugLogCombatMs < interval)
            {
                return;
            }

            self.LastDebugLogCombatMs = nowCombatMs;
        }

        private static void RecalculateHeightClampFromEnteredHeight(this AirComboComponent self)
        {
            float minY = self.EnteredHeight + self.MinHeightOffset;
            float maxY = self.EnteredHeight + self.MaxHeightOffset;

            if (maxY < minY)
            {
                float tmp = minY;
                minY = maxY;
                maxY = tmp;
            }

            // 合并：地板取更高，天花板取更低
            if (self.ComboMinHeight <= 0f)
            {
                self.ComboMinHeight = minY;
            }
            else
            {
                self.ComboMinHeight = Mathf.Max(self.ComboMinHeight, minY);
            }

            if (self.ComboMaxHeight <= 0f)
            {
                self.ComboMaxHeight = maxY;
            }
            else
            {
                self.ComboMaxHeight = Mathf.Min(self.ComboMaxHeight, maxY);
            }

            if (self.ComboMaxHeight < self.ComboMinHeight)
            {
                self.ComboMaxHeight = self.ComboMinHeight;
            }
        }
    }
}

