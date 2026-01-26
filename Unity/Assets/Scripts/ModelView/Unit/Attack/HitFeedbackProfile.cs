namespace ET
{
    /// <summary>
    /// 受击反馈 Profile（表现域策略）：
    /// - 目标侧 HitStop 是否启用/缩放
    /// - 震屏/慢动作是否启用/缩放（此版本先做数据结构与入口，不强绑定具体相机/时间系统）
    /// </summary>
    public readonly struct HitFeedbackProfile
    {
        /// <summary>是否允许“目标侧顿帧”（一般玩家=false，普通怪=true，Boss=false）。</summary>
        public readonly bool AllowVictimHitStop;
        /// <summary>目标侧顿帧缩放（1=不变，0=禁用）。</summary>
        public readonly float VictimHitStopScale;

        /// <summary>是否允许震屏（通常只对本地玩家相机）。</summary>
        public readonly bool AllowScreenShake;
        /// <summary>震屏强度缩放（1=不变）。</summary>
        public readonly float ScreenShakeScale;

        /// <summary>是否允许慢动作（通常由全局时间系统接管）。</summary>
        public readonly bool AllowTimeScale;
        /// <summary>慢动作强度缩放（1=不变）。</summary>
        public readonly float TimeScaleScale;

        public HitFeedbackProfile(
            bool allowVictimHitStop,
            float victimHitStopScale,
            bool allowScreenShake,
            float screenShakeScale,
            bool allowTimeScale,
            float timeScaleScale)
        {
            this.AllowVictimHitStop = allowVictimHitStop;
            this.VictimHitStopScale = victimHitStopScale;
            this.AllowScreenShake = allowScreenShake;
            this.ScreenShakeScale = screenShakeScale;
            this.AllowTimeScale = allowTimeScale;
            this.TimeScaleScale = timeScaleScale;
        }
    }
}