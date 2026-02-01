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
            self.ConsecutiveGroundedFrames = 0;
            self.ForceEndAfterGroundedFrames = Mathf.Max(1, self.ForceEndAfterGroundedFrames);
        }

        public static void Enter(this AirComboComponent self, long nowCombatMs, float currentY, in HitAirComboProfile profile)
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

            Log.Error($"进入空中连击状态: now={nowCombatMs}ms, currentY={currentY}, profile={profile.ToString()}");
            // 进入/再次进入：取消退出（命中续期不应被 ExitLerp 打断）
            self.IsExiting = false;
            self.ExitStartCombatMs = 0;

            // EnteredHeight：首次进入开启延迟捕获；再次进入不重置锚点，避免高度漂移导致 clamp 抖动
            if (!wasActive)
            {
                self.PendingCaptureEnteredHeight = true;
                // fallback：先记录当前高度（避免极端情况下永远不捕获）
                self.EnteredHeight = currentY;
            }

            // KeepAlive（至少维持一段时间）
            int minAirMs = Mathf.Max(0, profile.MinAirTimeMs);
            long nextEnd = nowCombatMs + minAirMs;
            if (!wasActive || nextEnd > self.EndCombatMs)
            {
                self.EndCombatMs = nextEnd;
            }

            // fail-safe：绝对上限只会变得更严格，不允许被不断延长
            int maxHangMs = Mathf.Max(minAirMs, profile.MaxTotalHangMs);
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

        public static void OnHit(this AirComboComponent self, long nowCombatMs, in HitAirComboProfile profile)
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

            // KeepAlive：取更长的结束点，但不得超过绝对上限
            long next = nowCombatMs + Mathf.Max(0, profile.MinAirTimeMs);
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
            self.DebugLog(nowCombatMs, "BeginExit");
        }

        public static void ForceEnd(this AirComboComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Active = false;
            self.IsExiting = false;
            self.PendingCaptureEnteredHeight = false;
            self.EndCombatMs = 0;
            self.AbsoluteEndCombatMs = 0;
            self.GravityScaleTarget = 1f;
            self.ExitFromGravityScale = 1f;
            self.ExitStartCombatMs = 0;
            self.ConsecutiveGroundedFrames = 0;
            self.LastDebugLogCombatMs = 0;
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

        public static void FailSafeTick(this AirComboComponent self, bool isGrounded)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!self.Active)
            {
                self.ConsecutiveGroundedFrames = 0;
                return;
            }

            if (isGrounded)
            {
                self.ConsecutiveGroundedFrames++;
                if (self.ConsecutiveGroundedFrames >= Mathf.Max(1, self.ForceEndAfterGroundedFrames))
                {
                    self.ForceEnd();
                }
            }
            else
            {
                self.ConsecutiveGroundedFrames = 0;
            }
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
            Log.Info($"[AirCombo] {reason} @ {nowCombatMs}ms :: {self}");
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

