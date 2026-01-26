namespace ET
{
    /// <summary>
    /// Profile 选择与默认值（先硬编码默认，后续可由表/资产覆盖）。
    /// </summary>
    public static class HitProfileLibrary
    {
        // ===== 默认 Rules =====
        public static readonly HitRulesProfile PlayerRules = new HitRulesProfile(
            HitReactionMask.All,
            lightPriority: 10,
            mediumPriority: 20,
            heavyPriority: 30,
            knockbackPriority: 40,
            knockupPriority: 50,
            knockdownPriority: 60,
            lowerOrEqualMode: HitRules.ApplyMode.Refresh,
            stunScale: 1f,           // 硬直倍率
            knockbackScale: 1f,      // 击退倍率
            knockupScale: 1f,        // 击飞倍率
            maxHitStunMs: 1200,      // 硬直上限(ms)
            maxKnockbackForce: 25f,  // 击退上限
            maxKnockupForce: 18f);   // 击飞上限

        public static readonly HitRulesProfile MonsterRules = new HitRulesProfile(
            HitReactionMask.All,
            lightPriority: 10,
            mediumPriority: 20,
            heavyPriority: 30,
            knockbackPriority: 40,
            knockupPriority: 50,
            knockdownPriority: 60,
            lowerOrEqualMode: HitRules.ApplyMode.Refresh,
            stunScale: 1f,
            knockbackScale: 1f,
            knockupScale: 1f,
            maxHitStunMs: 1500,
            maxKnockbackForce: 30f,
            maxKnockupForce: 22f);

        // Boss：默认不吃 Knockup/Knockdown，硬直缩放更低，避免被无限控制
        public static readonly HitRulesProfile BossRules = new HitRulesProfile(
            HitReactionMask.Light | HitReactionMask.Medium | HitReactionMask.Heavy | HitReactionMask.Knockback,
            lightPriority: 10,
            mediumPriority: 20,
            heavyPriority: 30,
            knockbackPriority: 40,
            knockupPriority: 0,
            knockdownPriority: 0,
            lowerOrEqualMode: HitRules.ApplyMode.Refresh,
            stunScale: 0.35f,         // Boss 硬直更短
            knockbackScale: 0.5f,      // Boss 击退更弱
            knockupScale: 0f,          // Boss 默认不击飞
            maxHitStunMs: 600,         // Boss 硬直上限更低
            maxKnockbackForce: 12f,    // Boss 击退上限更低
            maxKnockupForce: 0f);      // 0 表示完全禁用击飞

        // ===== 默认 Feedback =====
        // 玩家：目标侧 HitStop 通常不启用（避免被打时自己也顿帧割裂输入），更多应由“攻击者侧/镜头侧”产生反馈
        public static readonly HitFeedbackProfile PlayerFeedback = new HitFeedbackProfile(
            allowVictimHitStop: false,
            victimHitStopScale: 0f, // 目标侧顿帧禁用
            allowScreenShake: true,
            screenShakeScale: 1f,
            allowTimeScale: false,
            timeScaleScale: 0f);

        public static readonly HitFeedbackProfile MonsterFeedback = new HitFeedbackProfile(
            allowVictimHitStop: true,
            victimHitStopScale: 1f, // 目标侧顿帧正常
            allowScreenShake: false,
            screenShakeScale: 0f,
            allowTimeScale: false,
            timeScaleScale: 0f);

        public static readonly HitFeedbackProfile BossFeedback = new HitFeedbackProfile(
            allowVictimHitStop: false,
            victimHitStopScale: 0f, // Boss 不做目标侧顿帧
            allowScreenShake: true,
            screenShakeScale: 1.25f, // Boss 命中震屏更强（通常是“攻击者侧/镜头侧”实现）
            allowTimeScale: false,
            timeScaleScale: 0f);

        public static void Resolve(Unit unit, out HitRulesProfile rules, out HitFeedbackProfile feedback)
        {
            rules = MonsterRules;
            feedback = MonsterFeedback;

            if (unit == null)
            {
                return;
            }

            switch (unit.UnitType())
            {
                case UnitType.Player:
                    rules = PlayerRules;
                    feedback = PlayerFeedback;
                    return;
                case UnitType.Monster:
                {
                    var mi = unit.GetComponent<MonsterIdentityComponent>();
                    if (mi != null && mi.IsBoss)
                    {
                        rules = BossRules;
                        feedback = BossFeedback;
                        return;
                    }
                    rules = MonsterRules;
                    feedback = MonsterFeedback;
                    return;
                }
                default:
                    return;
            }
        }
    }
}

