using UnityEngine;

namespace ET
{
    /// <summary>
    /// Profile 选择与默认值（先硬编码默认，后续可由表/资产覆盖）。
    /// </summary>
    public static class HitReactionProfileProvider
    {
        // ===== 默认 Rules =====
        public static readonly HitReactionRulesConfig PlayerReactionRules = new HitReactionRulesConfig(
            allowedReactionGroups: HitReactionGroup.All,
            allowedStateVisuals: HitStateVisualMask.All,
            hitInterruptThresholdsTable: new HitReactionRulesConfig.HitInterruptThresholdsTable(
                grounded: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 20, knockbackThreshold: 30, airborneThreshold: 50, knockdownThreshold: 60, getUpInterruptThreshold: 255),
                airborne: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 10, knockbackThreshold: 20, airborneThreshold: 255, knockdownThreshold: 40, getUpInterruptThreshold: 255),
                airFinisher: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 20, getUpInterruptThreshold: 255),
                knockdown: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 80),
                getUp: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 80)),
            scales: new HitReactionRulesConfig.Scales(1f, 1f, 1f),
            limits: new HitReactionRulesConfig.Limits(1200, 25f, 18f));

        public static readonly HitReactionRulesConfig MonsterReactionRules = new HitReactionRulesConfig(
            allowedReactionGroups: HitReactionGroup.All,
            allowedStateVisuals: HitStateVisualMask.All,
            hitInterruptThresholdsTable: new HitReactionRulesConfig.HitInterruptThresholdsTable(
                grounded: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 20, knockbackThreshold: 30, airborneThreshold: 50, knockdownThreshold: 60, getUpInterruptThreshold: 255),
                airborne: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 10, knockbackThreshold: 20, airborneThreshold: 70, knockdownThreshold: 60, getUpInterruptThreshold: 255),
                airFinisher: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 65, getUpInterruptThreshold: 255),
                knockdown: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 80),
                getUp: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 80)),
            scales: new HitReactionRulesConfig.Scales(1f, 1f, 1f),
            limits: new HitReactionRulesConfig.Limits(1500, 30f, 22f));

        // Boss：默认不吃 Stagger/Stun，硬直缩放更低，避免被无限控制
        public static readonly HitReactionRulesConfig BossReactionRules = new HitReactionRulesConfig(
            allowedReactionGroups: HitReactionGroup.Minor | HitReactionGroup.Major, // 禁播 Control 表现（但规则仍可生效/会降级表现）
            allowedStateVisuals: HitStateVisualMask.All,
            hitInterruptThresholdsTable: new HitReactionRulesConfig.HitInterruptThresholdsTable(
                grounded: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 25, knockbackThreshold: 45, airborneThreshold: 55, knockdownThreshold: 65, getUpInterruptThreshold: 255),
                airborne: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 255),
                airFinisher: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 255),
                knockdown: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 255),
                getUp: new HitReactionRulesConfig.HitInterruptThresholds(lightReactionThreshold: 255, knockbackThreshold: 255, airborneThreshold: 255, knockdownThreshold: 255, getUpInterruptThreshold: 255)),
            scales: new HitReactionRulesConfig.Scales(0.35f, 0.5f, 0.5f),
            limits: new HitReactionRulesConfig.Limits(600, 12f, 10f));

        // ===== 默认 Feedback =====
        // 玩家：目标侧 HitStop 通常不启用（避免被打时自己也顿帧割裂输入），更多应由“攻击者侧/镜头侧”产生反馈
        public static readonly HitFeedbackConfig PlayerFeedback = new HitFeedbackConfig(
            new HitFeedbackConfig.Options(
                allowVictimHitStop: false,
                victimHitStopScale: 0f,
                allowScreenShake: true,
                screenShakeScale: 1f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        public static readonly HitFeedbackConfig MonsterFeedback = new HitFeedbackConfig(
            new HitFeedbackConfig.Options(
                allowVictimHitStop: true,
                victimHitStopScale: 1f,
                allowScreenShake: false,
                screenShakeScale: 0f,
                allowTimeScale: false,
                timeScaleScale: 0f));

        public static readonly HitFeedbackConfig BossFeedback = new HitFeedbackConfig(
            new HitFeedbackConfig.Options(
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
            landingStunMs: 180,
            maxAirborneKnockupForce: 2.5f,
            maxHorizontalDistance:3,
            maxHorizontalSpeed: 2.5f,
            recenterStrength:2.5f,
            recenterDeadZone: 1.5f);

        public static readonly HitAirComboProfile MonsterAirCombo = new HitAirComboProfile(
            enable: true,
            minAirTimeMs: 260,
            maxTotalHangMs: 4500,
            gravityScaleDuringCombo: 0.10f,
            minFallSpeedAbs: 0.7f,
            minHeightOffset: 0f,
            maxHeightOffset: 2.5f,
            exitLerpMs: 170,
            landingStunMs: 200,
            maxAirborneKnockupForce: 2.5f,
            maxHorizontalDistance:3,
            maxHorizontalSpeed: 2.5f,
            recenterStrength:4.5f,
            recenterDeadZone: 1.5f);

        public static readonly HitAirComboProfile BossAirCombo = new HitAirComboProfile(
            enable: false,
            minAirTimeMs: 0,
            maxTotalHangMs: 0,
            gravityScaleDuringCombo: 1f,
            minFallSpeedAbs: 0f,
            minHeightOffset: 0f,
            maxHeightOffset: 0f,
            exitLerpMs: 0,
            landingStunMs: 0,
            maxAirborneKnockupForce: 0f,
            maxHorizontalDistance:3,
            maxHorizontalSpeed: 2.5f,
            recenterStrength:2.5f,
            recenterDeadZone: 1.5f);

        public static void ResolveConfig(Unit unit, out HitReactionRulesConfig reactionRules, out HitFeedbackConfig feedback)
        {
            reactionRules = MonsterReactionRules;
            feedback = MonsterFeedback;

            if (unit == null)
            {
                return;
            }

            switch (unit.UnitType())
            {
                case UnitType.Player:
                    reactionRules = PlayerReactionRules;
                    feedback = PlayerFeedback;
                    return;
                case UnitType.Monster:
                {
                    var mi = unit.GetComponent<MonsterIdentityComponent>();
                    if (mi != null && mi.IsBoss)
                    {
                        reactionRules = BossReactionRules;
                        feedback = BossFeedback;
                        return;
                    }
                    reactionRules = MonsterReactionRules;
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
        
        public static AirborneReason ResolveAirborneReasonForRequest(in HitReactionRequest request)
        {
            // Slam 通常意味着“砸地/击倒”语义；Launch 表示击飞；Refresh(空中追击)默认 Juggled

            AirborneReason type = AirborneReason.None;
            switch (request.MotionData.MotionType)
            {
                case HitMotionType.Normal:
                case HitMotionType.Knockback:
                case HitMotionType.PullTowardAttacker:
                    type = AirborneReason.Juggled;
                    break;
                case HitMotionType.Knockup:
                    type = AirborneReason.Launched;
                    break;
                case HitMotionType.KnockDown:
                    type = AirborneReason.Knockdown;
                    break;
            }

            return type;
        }
        
        public static HitReactionRequest From(in HitEffectData effect, in HitFeedbackData feedback, Vector3 hitDirection, int defaultHitStopMs, int attackerSegmentComboTimeoutMs = 0, float attackRadius = 0f, Vector3 attackerWorldPos = default)
        {
            // ModelView 只存数据：Priority=0 表示未配置。
            byte priority = effect.HitStrength != 0 ? effect.HitStrength : GetDefaultPriority(effect.HitReaction);

            return new HitReactionRequest(effect, feedback, hitDirection, defaultHitStopMs, attackerSegmentComboTimeoutMs, attackRadius, attackerWorldPos);
        }
        
        private static byte GetDefaultPriority(HitReactionType type)
        {
            // 仅作为“未配置 Priority 的兜底规则”，数值可以后续完全由策划配置覆盖。
            switch (type)
            {
                case HitReactionType.MinorHit: return 10;
                case HitReactionType.MediumHit: return 20;
                case HitReactionType.MajorHit: return 40;
                case HitReactionType.StaggerHit: return 60;
                case HitReactionType.StunHit: return 80;
                default: return 0;
            }
        }
        
        public static HitReactionRequest Normalize(this HitReactionComponent self, in HitReactionRequest r, in HitReactionRulesConfig config)
        {
            Vector3 dir = r.HitDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
            }

            int stun = Mathf.Max(0, r.HitStunMs);
            if (config.Scale.Stun > 0f && !Mathf.Approximately(config.Scale.Stun, 1f))
            {
                stun = Mathf.RoundToInt(stun * config.Scale.Stun);
            }
            if (config.Limit.MaxHitStunMs > 0 && stun > config.Limit.MaxHitStunMs)
            {
                stun = config.Limit.MaxHitStunMs;
            }

            // 物理轨道归一化
            HitMotionData motion = r.MotionData;
            motion.Force = Mathf.Max(0f, motion.Force);
            
            // 根据 Profile 缩放和限制力
            if (motion.MotionType == HitMotionType.Knockback || motion.MotionType == HitMotionType.PullTowardAttacker)
            {
                if (config.Scale.Knockback > 0f && !Mathf.Approximately(config.Scale.Knockback, 1f)) motion.Force *= config.Scale.Knockback;
                if (config.Limit.MaxKnockbackForce > 0f && motion.Force > config.Limit.MaxKnockbackForce) motion.Force = config.Limit.MaxKnockbackForce;
            }
            else if (motion.MotionType == HitMotionType.Knockup || motion.MotionType == HitMotionType.KnockDown)
            {
                if (config.Scale.Knockup > 0f && !Mathf.Approximately(config.Scale.Knockup, 1f)) motion.Force *= config.Scale.Knockup;
                if (config.Limit.MaxKnockupForce >= 0f && motion.Force > config.Limit.MaxKnockupForce) motion.Force = config.Limit.MaxKnockupForce;
            }

            int hitStop = Mathf.Max(0, r.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, r.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, r.ScreenShakeDurationMs);
            float timeScale = r.TimeScale <= 0f ? 1f : r.TimeScale;
            int timeScaleMs = Mathf.Max(0, r.TimeScaleDurationMs);

            return new HitReactionRequest(
                reactionType: r.ReactionType,
                hitStrength: r.HitStrength,
                motionData: motion,
                targetStates: r.TargetStates,
                hitDirection: dir,
                hitStunMs: stun,
                victimHitStopMs: hitStop,
                screenShakeIntensity: shakeIntensity,
                screenShakeDurationMs: shakeDurationMs,
                timeScale: timeScale,
                timeScaleDurationMs: timeScaleMs,
                attackerSegmentComboTimeoutMs: r.AttackerSegmentComboTimeoutMs,
                attackRadius: r.AttackRadius,
                attackerWorldPos: r.AttackerWorldPos);
        }
        
        public static bool PassTargetStateFilter(this HitReactionComponent self, TargetStateMask filter)
        {
            if (filter == TargetStateMask.Any)
            {
                return true;
            }

            TargetStateMask current;
            if (self.IsKnockdown)
            {
                current = TargetStateMask.Knockdown;
            }
            else if (self.IsAirborne)
            {
                current = TargetStateMask.Airborne;
            }
            else
            {
                current = TargetStateMask.Grounded;
            }

            return (filter & current) != 0;
        }
        
        /// <summary>
        /// 过滤空中受击时的运动类型。Knockup 根据 allowSecondaryKnockup 和 MaxAirborneKnockupForce 决定是否允许、以及力上限。
        /// </summary>
        /// <param name="allowSecondaryKnockup">Airborne 时为 true 允许 capped 二次击飞；AirFinisher 时为 false 完全禁止。</param>
        public static HitReactionRequest FilterAirborneMotion(this HitReactionComponent self, in HitReactionRequest request, bool allowSecondaryKnockup = true)
        {
            if (request.MotionData.MotionType != HitMotionType.Knockup)
            {
                return request;
            }

            if (!allowSecondaryKnockup)
            {
                // 完全禁止二次击飞
                HitMotionData m = request.MotionData;
                m.MotionType = HitMotionType.Normal;
                m.Force = 0f;
                m.DurationMs = 0;
                return BuildFilteredRequest(in request, m);
            }

            ResolveAirCombo(self.OwnerUnit, out var profile);
            if (profile.MaxAirborneKnockupForce <= 0f)
            {
                // 配置为 0：禁止
                HitMotionData m = request.MotionData;
                m.MotionType = HitMotionType.Normal;
                m.Force = 0f;
                m.DurationMs = 0;
                return BuildFilteredRequest(in request, m);
            }

            // 允许二次击飞，夹持力上限
            HitMotionData capped = request.MotionData;
            capped.Force = Mathf.Min(capped.Force, profile.MaxAirborneKnockupForce);
            return BuildFilteredRequest(in request, capped);
        }

        private static HitReactionRequest BuildFilteredRequest(in HitReactionRequest request, HitMotionData motionData)
        {
            return new HitReactionRequest(
                reactionType: request.ReactionType,
                hitStrength: request.HitStrength,
                motionData: motionData,
                targetStates: request.TargetStates,
                hitDirection: request.HitDirection,
                hitStunMs: request.HitStunMs,
                victimHitStopMs: request.VictimHitStopMs,
                screenShakeIntensity: request.ScreenShakeIntensity,
                screenShakeDurationMs: request.ScreenShakeDurationMs,
                timeScale: request.TimeScale,
                timeScaleDurationMs: request.TimeScaleDurationMs,
                attackerSegmentComboTimeoutMs: request.AttackerSegmentComboTimeoutMs,
                attackRadius: request.AttackRadius,
                attackerWorldPos: request.AttackerWorldPos);
        }
        
        private static HitReactionGroup ToGroup(HitReactionType type)
        {
            return type switch
            {
                HitReactionType.MinorHit => HitReactionGroup.Minor,
                HitReactionType.MediumHit => HitReactionGroup.Minor,
                HitReactionType.MajorHit => HitReactionGroup.Major,
                HitReactionType.StaggerHit => HitReactionGroup.Control,
                HitReactionType.StunHit => HitReactionGroup.Control,
                _ => HitReactionGroup.None
            };
        }

        public static HitReactionType DegradeReactionType(this HitReactionComponent self,HitReactionType desired, HitReactionGroup allowed)
        {
            if (desired == HitReactionType.None)
            {
                return HitReactionType.None;
            }

            HitReactionGroup g = ToGroup(desired);
            if (g != HitReactionGroup.None && (allowed & g) != 0)
            {
                return desired;
            }

            // 降级链：Control → Major → Minor → None
            if (g == HitReactionGroup.Control)
            {
                return self.DegradeReactionType(HitReactionType.MajorHit, allowed);
            }
            if (g == HitReactionGroup.Major)
            {
                return self.DegradeReactionType(HitReactionType.MediumHit, allowed);
            }
            if (g == HitReactionGroup.Minor)
            {
                return HitReactionType.None;
            }

            return HitReactionType.None;
        }
    }
}

