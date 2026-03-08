namespace ET
{
    /// <summary>
    /// 空中连段状态（AirComboState）：管理空中连段的激活/退出/超时。
    /// - 进入：上挑/击飞（HitState 进入 Airborne 且 profile.Enable）
    /// - 维持：命中续期（OnHit）
    /// - 退出：超时/连段结束 -> 自然落地
    /// 重力缩放由 CharacterControllerComponent 直接读取 CachedAirCombo Profile。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class AirComboComponent : Entity, IAwake
    {
        // ===== 状态 =====
        public bool Active;
        public bool IsExiting;

        /// <summary>
        /// 空中结束时间 => 硬直结束时间 + MaxAirOffsetMs
        /// </summary>
        public long AirEndCombatMs;

        public override string ToString()
        {
            return $"AirCombo(Active={Active}, Exiting={IsExiting}, AirEnd={AirEndCombatMs})";
        }
    }
}