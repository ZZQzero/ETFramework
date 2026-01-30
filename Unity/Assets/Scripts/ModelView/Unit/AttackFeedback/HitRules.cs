using UnityEngine;

namespace ET
{
    /// <summary>
    /// - 负责“能否受击/是否通过过滤/受击优先级/参数归一化”等判定
    /// - 只产出决策，不直接操作动画/位移/组件
    /// </summary>
    public static class HitRules
    {
        public enum ApplyMode : byte
        {
            Reject = 0,
            Replace = 1, // 直接覆盖当前受击（切状态/播新动画）
            Refresh = 2, // 不切状态，只刷新计时/力度（例如硬直延长）
            FeedbackOnly = 3, // 仅触发反馈（HitStop/震屏/慢动作等），不进入受击状态机
        }

        public readonly struct Result
        {
            public readonly ApplyMode Mode;
            public readonly HitReactionRequest Request; // 归一化后的 request（方向/数值 clamp）

            public bool Accepted => this.Mode != ApplyMode.Reject;

            public Result(ApplyMode mode, HitReactionRequest request)
            {
                this.Mode = mode;
                this.Request = request;
            }

            public override string ToString()
            {
                return $"受击判定结果(接受={this.Accepted}, 模式={this.Mode}, {this.Request})";
            }
        }

        public static Result Evaluate(HitReactionComponent target, in HitReactionRequest incoming, in HitRulesProfile profile)
        {
            if (target == null || target.IsDisposed)
                return new Result(ApplyMode.Reject, incoming);

            if (target.Owner == null || target.OwnerUnit == null)
                return new Result(ApplyMode.Reject, incoming);

            bool hasVisualOrPhysical = incoming.ReactionType != HitReactionType.None || incoming.MotionData.MotionType != HitMotionType.None;
            bool hasAnyFeedback =
                incoming.VictimHitStopMs > 0 ||
                incoming.ScreenShakeIntensity > 0f ||
                incoming.ScreenShakeDurationMs > 0 ||
                (incoming.TimeScale > 0f && !Mathf.Approximately(incoming.TimeScale, 1f)) ||
                incoming.TimeScaleDurationMs > 0;

            // 允许“仅反馈”的请求（例如格挡成功、护盾命中）：不进入受击状态机
            // 注意：此分支不参与 AcceptMask/TargetStates/CanBeHit 判定，避免把表现反馈耦合进 gameplay 受击规则。
            if (!hasVisualOrPhysical)
            {
                if (!hasAnyFeedback)
                {
                    return new Result(ApplyMode.Reject, incoming);
                }
                HitReactionRequest normalizedFeedbackOnly = Normalize(incoming, in profile);
                return new Result(ApplyMode.FeedbackOnly, normalizedFeedbackOnly);
            }

            // 状态检查：起身、倒地等状态可能禁止受击
            if (!target.CanBeHit)
                return new Result(ApplyMode.Reject, incoming);

            if (!PassTargetStateFilter(target, incoming.TargetStates))
                return new Result(ApplyMode.Reject, incoming);

            // Profile 过滤：检查是否在接受列表中
            if (!profile.Accepts(incoming.ReactionType))
                return new Result(ApplyMode.Reject, incoming);

            // 归一化参数（应用 Scale 和 Limit）
            HitReactionRequest normalized = Normalize(incoming, in profile);

            // 1. 获取本次攻击强度
            int incomingPriority = profile.GetPriority(normalized.ReactionType);

            // 2. 如果当前没有受击：直接替换（启动）
            if (!target.IsInHitReaction)
                return new Result(ApplyMode.Replace, normalized);

            // 3. 判定应用模式 (Apply Mode Resolve)
            // 获取上一次命中的招式强度
            int lastAttackPriority = profile.GetPriority(target.CurrentReactionType);

            ApplyMode intendedMode = incomingPriority > lastAttackPriority ? ApplyMode.Replace : profile.LowerOrEqualMode;

            // 4. 核心：状态抵抗力（只限制 Replace；空中连段允许弱招 Refresh 续期）
            int currentStateResistance = profile.GetResistance(target.CurrentState);
            if (incomingPriority < currentStateResistance)
            {
                // 霸体/抗性：弱招无法打断（Replace 禁止）
                if (intendedMode == ApplyMode.Replace)
                {
                    return new Result(ApplyMode.Reject, normalized);
                }

                // 关键：空中连段阶段允许弱招 Refresh（续硬直/续空中时间）
                if (target.CurrentState == HitState.Airborne || target.CurrentState == HitState.Falling)
                {
                    var airCombo = target.OwnerUnit.GetComponent<AirComboComponent>();
                    if (airCombo != null && airCombo.Active)
                    {
                        return new Result(ApplyMode.Refresh, normalized);
                    }
                }

                // 倒地/起身等状态仍严格（防无限控）
                return new Result(ApplyMode.Reject, normalized);
            }

            return new Result(intendedMode, normalized);
        }

        private static HitReactionRequest Normalize(in HitReactionRequest r, in HitRulesProfile profile)
        {
            Vector3 dir = r.HitDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
            }

            int stun = Mathf.Max(0, r.HitStunMs);
            if (profile.Scale.Stun > 0f && !Mathf.Approximately(profile.Scale.Stun, 1f))
            {
                stun = Mathf.RoundToInt(stun * profile.Scale.Stun);
            }
            if (profile.Limit.MaxHitStunMs > 0 && stun > profile.Limit.MaxHitStunMs)
            {
                stun = profile.Limit.MaxHitStunMs;
            }

            // 物理轨道归一化
            HitMotionData motion = r.MotionData;
            motion.Force = Mathf.Max(0f, motion.Force);
            
            // 根据 Profile 缩放和限制力
            if (motion.MotionType == HitMotionType.Push || motion.MotionType == HitMotionType.Pull)
            {
                if (profile.Scale.Knockback > 0f && !Mathf.Approximately(profile.Scale.Knockback, 1f)) motion.Force *= profile.Scale.Knockback;
                if (profile.Limit.MaxKnockbackForce > 0f && motion.Force > profile.Limit.MaxKnockbackForce) motion.Force = profile.Limit.MaxKnockbackForce;
            }
            else if (motion.MotionType == HitMotionType.Launch || motion.MotionType == HitMotionType.Slam)
            {
                if (profile.Scale.Knockup > 0f && !Mathf.Approximately(profile.Scale.Knockup, 1f)) motion.Force *= profile.Scale.Knockup;
                if (profile.Limit.MaxKnockupForce >= 0f && motion.Force > profile.Limit.MaxKnockupForce) motion.Force = profile.Limit.MaxKnockupForce;
            }

            int hitStop = Mathf.Max(0, r.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, r.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, r.ScreenShakeDurationMs);
            float timeScale = r.TimeScale <= 0f ? 1f : r.TimeScale;
            int timeScaleMs = Mathf.Max(0, r.TimeScaleDurationMs);

            HitReactionRequest.FeedbackPayload fp = new HitReactionRequest.FeedbackPayload(
                victimHitStopMs: hitStop,
                screenShakeIntensity: shakeIntensity,
                screenShakeDurationMs: shakeDurationMs,
                timeScale: timeScale,
                timeScaleDurationMs: timeScaleMs);

            return new HitReactionRequest(
                r.ReactionType,
                motion,
                r.TargetStates,
                dir,
                stun,
                in fp);
        }

        private static bool PassTargetStateFilter(HitReactionComponent target, TargetStateMask filter)
        {
            if (filter == TargetStateMask.Any)
            {
                return true;
            }

            TargetStateMask current;
            if (target.IsKnockdown)
            {
                current = TargetStateMask.Knockdown;
            }
            else if (target.IsAirborne)
            {
                current = TargetStateMask.Airborne;
            }
            else
            {
                current = TargetStateMask.Grounded;
            }

            return (filter & current) != 0;
        }
    }
}

