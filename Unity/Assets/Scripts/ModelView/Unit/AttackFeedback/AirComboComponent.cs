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
        /// <summary>
        /// 空中结束时间 => 地面硬值时间 + offset
        /// </summary>
        public long AirEndCombatMs;

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

        // 注：水平距离约束（ComboCenterWorldPos, MaxHorizontalDistance 等）已迁移到
        // HitReactionComponent.TetherAnchorPos + HitTetherProfile 统一管理（地面+空中共用）。
        
        public override string ToString()
        {
            return
                $"AirCombo(Active={Active}, Exiting={IsExiting}, " +
                $"gTarget={GravityScaleTarget:0.###}, minFall={MinFallSpeed:0.###}, ExitLerpMs={ExitLerpMs})";
        }
    }
}