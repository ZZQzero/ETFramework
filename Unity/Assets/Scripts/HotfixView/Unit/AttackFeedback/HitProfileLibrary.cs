namespace ET
{
    /// <summary>
    /// Profile 选择与默认值（先硬编码默认，后续可由表/资产覆盖）。
    /// </summary>
    public static class HitProfileLibrary
    {
        // ===== 默认 Rules =====
        public static readonly HitRulesProfile PlayerRules = new HitRulesProfile(
            acceptMask: HitReactionMask.All,
            priorities: new HitRulesProfile.Priorities(10, 20, 30, 40, 50),
            resistances: HitRulesProfile.Resistances.Default,
            scales: new HitRulesProfile.Scales(1f, 1f, 1f),
            limits: new HitRulesProfile.Limits(1200, 25f, 18f));

        public static readonly HitRulesProfile MonsterRules = new HitRulesProfile(
            acceptMask: HitReactionMask.All,
            priorities: new HitRulesProfile.Priorities(10, 20, 30, 40, 50),
            resistances: HitRulesProfile.Resistances.Default,
            scales: new HitRulesProfile.Scales(1f, 1f, 1f),
            limits: new HitRulesProfile.Limits(1500, 30f, 22f));

        // Boss：默认不吃 Stagger/Stun，硬直缩放更低，避免被无限控制
        public static readonly HitRulesProfile BossRules = new HitRulesProfile(
            acceptMask: HitReactionMask.Light | HitReactionMask.Medium | HitReactionMask.Heavy,
            priorities: new HitRulesProfile.Priorities(10, 20, 30, 0, 0),
            resistances: new HitRulesProfile.Resistances(25, 45, 55, 65, 85), 
            scales: new HitRulesProfile.Scales(0.35f, 0.5f, 0.5f),
            limits: new HitRulesProfile.Limits(600, 12f, 10f));

        // ===== 默认 Feedback =====
        // 玩家：目标侧 HitStop 通常不启用（避免被打时自己也顿帧割裂输入），更多应由“攻击者侧/镜头侧”产生反馈
        public static readonly HitFeedbackProfile PlayerFeedback = new HitFeedbackProfile(
            new HitFeedbackProfile.Options(
                allowVictimHitStop: false,
                victimHitStopScale: 0f,
                allowScreenShake: true,
                screenShakeScale: 1f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        public static readonly HitFeedbackProfile MonsterFeedback = new HitFeedbackProfile(
            new HitFeedbackProfile.Options(
                allowVictimHitStop: true,
                victimHitStopScale: 1f,
                allowScreenShake: false,
                screenShakeScale: 0f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        public static readonly HitFeedbackProfile BossFeedback = new HitFeedbackProfile(
            new HitFeedbackProfile.Options(
                allowVictimHitStop: false,
                victimHitStopScale: 0f,
                allowScreenShake: true,
                screenShakeScale: 1.25f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        // ===== 默认 AirCombo（Combo Physics） =====
        // 说明：
        // - Player/Monster 默认启用（由用户选择 monster_and_player）
        // - Boss 默认禁用或更短/更重（避免被无限挂空）
        public static readonly HitAirComboProfile PlayerAirCombo = new HitAirComboProfile(
            enable: true,
            minAirTimeMs: 250,
            maxTotalHangMs: 3500,
            gravityScaleDuringCombo: 0.12f,
            minFallSpeedAbs: 0.8f,
            minHeightOffset: 0f,
            maxHeightOffset: 2.2f,
            exitLerpMs: 160,
            landingStunMs: 180);

        public static readonly HitAirComboProfile MonsterAirCombo = new HitAirComboProfile(
            enable: true,
            minAirTimeMs: 260,
            maxTotalHangMs: 4500,
            gravityScaleDuringCombo: 0.10f,
            minFallSpeedAbs: 0.7f,
            minHeightOffset: 0f,
            maxHeightOffset: 2.5f,
            exitLerpMs: 170,
            landingStunMs: 200);

        public static readonly HitAirComboProfile BossAirCombo = new HitAirComboProfile(
            enable: false,
            minAirTimeMs: 0,
            maxTotalHangMs: 0,
            gravityScaleDuringCombo: 1f,
            minFallSpeedAbs: 0f,
            minHeightOffset: 0f,
            maxHeightOffset: 0f,
            exitLerpMs: 0,
            landingStunMs: 0);

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

        public static void ResolveAirCombo(Unit unit, out HitAirComboProfile airCombo)
        {
            airCombo = MonsterAirCombo;
            if (unit == null)
            {
                return;
            }

            switch (unit.UnitType())
            {
                case UnitType.Player:
                    airCombo = PlayerAirCombo;
                    return;
                case UnitType.Monster:
                {
                    var mi = unit.GetComponent<MonsterIdentityComponent>();
                    if (mi != null && mi.IsBoss)
                    {
                        airCombo = BossAirCombo;
                        return;
                    }
                    airCombo = MonsterAirCombo;
                    return;
                }
                default:
                    return;
            }
        }
    }
}

