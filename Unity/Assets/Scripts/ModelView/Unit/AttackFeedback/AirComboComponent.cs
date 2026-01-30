using UnityEngine;

namespace ET
{
    /// <summary>
    /// 空中连段状态（AirComboState）：这是唯一决定“单位会不会掉下来”的地方。
    /// - 进入：上挑/击飞（HitState 进入 Airborne 且 profile.Enable）
    /// - 维持：命中续期（KeepAlive）
    /// - 退出：超时/连段结束 -> 重力 lerp 回 1 -> 自然落地
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class AirComboComponent : Entity, IAwake
    {
        // ===== 状态 =====
        public bool Active;
        public bool IsExiting;

        /// <summary>本次空中连段预计结束时间（combat-time）。</summary>
        public long EndCombatMs;

        /// <summary>fail-safe：悬空总时长上限（combat-time）。</summary>
        public long AbsoluteEndCombatMs;

        /// <summary>进入空中连段后第一帧（GroundBreak 生效后）捕获进入高度。</summary>
        public bool PendingCaptureEnteredHeight;

        /// <summary>进入时高度（用于 min/max clamp 的锚）。</summary>
        public float EnteredHeight;

        /// <summary>高度夹持下限/上限（世界 Y）。</summary>
        public float ComboMinHeight;
        public float ComboMaxHeight;

        /// <summary>空中连段目标重力缩放（0~1）。</summary>
        public float GravityScaleTarget = 1f;

        /// <summary>退出时的起始重力缩放（用于 lerp）。</summary>
        public float ExitFromGravityScale = 1f;

        /// <summary>退出开始时间（combat-time）。</summary>
        public long ExitStartCombatMs;

        /// <summary>退出 lerp 时长（combat-time）。</summary>
        public int ExitLerpMs = 150;

        /// <summary>最小下落速度下限（负数，m/s）。例如 -1。</summary>
        public float MinFallSpeed = -1f;

        /// <summary>进入时使用的高度偏移（相对 EnteredHeight），用于延迟捕获后计算 clamp。</summary>
        public float MinHeightOffset;
        public float MaxHeightOffset;

        /// <summary>落地硬直（非砸地/倒地语义）。</summary>
        public int LandingStunMs;

        /// <summary>调试：可选开关。</summary>
        public bool DebugEnabled;

        /// <summary>调试日志最小间隔（combat-time）。</summary>
        public int DebugLogIntervalMs = 200;

        /// <summary>上一次输出调试日志的 combat-time。</summary>
        public long LastDebugLogCombatMs;

        /// <summary>fail-safe：Active 期间连续判定为地面的帧数。</summary>
        public int ConsecutiveGroundedFrames;

        /// <summary>fail-safe：连续地面帧达到该阈值后强制结束（避免状态机卡死）。</summary>
        public int ForceEndAfterGroundedFrames = 3;

        public override string ToString()
        {
            return
                $"AirCombo(Active={Active}, Exiting={IsExiting}, End={EndCombatMs}, AbsEnd={AbsoluteEndCombatMs}, " +
                $"EnteredY={EnteredHeight:0.###}, MinY={ComboMinHeight:0.###}, MaxY={ComboMaxHeight:0.###}, " +
                $"gTarget={GravityScaleTarget:0.###}, minFall={MinFallSpeed:0.###}, ExitLerpMs={ExitLerpMs})";
        }
    }
}

