namespace ET
{
    /// <summary>
    /// 受击反馈 Profile（表现域策略）：
    /// - 目标侧 HitStop 是否启用/缩放
    /// - 震屏/慢动作是否启用/缩放（此版本先做数据结构与入口，不强绑定具体相机/时间系统）
    /// </summary>
    public readonly struct HitFeedbackConfig
    {
        /// <summary>
        /// 反馈配置分组
        /// </summary>
        public readonly struct Options
        {
            public readonly bool AllowVictimHitStop;
            public readonly float VictimHitStopScale;
            public readonly bool AllowScreenShake;
            public readonly float ScreenShakeScale;
            public readonly bool AllowTimeScale;
            public readonly float TimeScaleScale;

            public Options(
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

        /// <summary>所有反馈配置选项。</summary>
        public readonly Options Option;

        public HitFeedbackConfig(in Options options)
        {
            this.Option = options;
        }

        public override string ToString()
        {
            return $"受击反馈配置(受击停顿={this.Option.AllowVictimHitStop}, 缩放={this.Option.VictimHitStopScale:0.###}; 震屏={this.Option.AllowScreenShake}, 缩放={this.Option.ScreenShakeScale:0.###}; 时间缩放={this.Option.AllowTimeScale}, 缩放={this.Option.TimeScaleScale:0.###})";
        }
    }
}