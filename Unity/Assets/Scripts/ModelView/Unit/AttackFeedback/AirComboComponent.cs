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
        /// <summary>
        /// 空中连段退出完成事件。
        /// </summary>
        public EventHook OnExitCompleted;

        // ===== 状态 =====
        public bool Active;
        public bool IsExiting;

        /// <summary>本次空中连段预计结束时间（combat-time）。</summary>
        public long EndCombatMs;

        /// <summary>fail-safe：悬空总时长上限（combat-time）。</summary>
        public long AbsoluteEndCombatMs;

        /// <summary>高度夹持锚点（攻击者 Y），用于 min/max clamp 计算。</summary>
        public float EnteredHeight;

        /// <summary>高度夹持下限/上限（世界 Y）。</summary>
        public float ComboMinHeight;
        public float ComboMaxHeight;
        
        /// <summary>高度夹持是否已初始化（避免 Y<=0 场景下的误判）。</summary>
        public bool HeightClampInitialized;

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
        public bool DebugEnabled = true;

        /// <summary>调试日志最小间隔（combat-time）。</summary>
        public int DebugLogIntervalMs = 200;

        /// <summary>上一次输出调试日志的 combat-time。</summary>
        public long LastDebugLogCombatMs;

        /// <summary>
        /// fail-safe：连续地面帧阈值（达到该值后强制结束，避免状态机卡死）。
        /// 实际帧数由 CheckGroundedComponent.ConsecutiveGroundedFrames 提供，避免重复计数。
        /// </summary>
        public int ForceEndGroundedFramesThreshold = 3;
        
        // ===== 空中连击横向控制 =====

        // 连段中心（世界坐标，XZ 锚点）
        public Vector3 ComboCenterWorldPos;

        // 最大允许的横向偏移半径
        public float MaxHorizontalDistance;

        // 最大横向速度（XZ）
        public float MaxHorizontalSpeed;

        // 回拉强度（速度 = 超出距离 * Strength）
        public float RecenterStrength;

        // 回拉死区（小于该距离不拉）
        public float RecenterDeadZone;
        
        public override string ToString()
        {
            return
                $"AirCombo(Active={Active}, Exiting={IsExiting}, End={EndCombatMs}, AbsEnd={AbsoluteEndCombatMs}, " +
                $"EnteredY={EnteredHeight:0.###}, MinY={ComboMinHeight:0.###}, MaxY={ComboMaxHeight:0.###}, " +
                $"gTarget={GravityScaleTarget:0.###}, minFall={MinFallSpeed:0.###}, ExitLerpMs={ExitLerpMs})";
        }
    }
}

