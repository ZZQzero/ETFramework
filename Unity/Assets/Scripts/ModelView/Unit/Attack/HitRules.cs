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
        }

        public static Result Evaluate(HitReactionComponent target, in HitReactionRequest incoming, in HitRulesProfile profile)
        {
            if (target == null || target.IsDisposed)
                return new Result(ApplyMode.Reject, incoming);

            if (target.Owner == null || target.OwnerUnit == null)
                return new Result(ApplyMode.Reject, incoming);

            if (incoming.ReactionType == HitReactionType.None)
                return new Result(ApplyMode.Reject, incoming);

            // GetUp 默认不可受击（组件属性定义）
            if (!target.CanBeHit)
                return new Result(ApplyMode.Reject, incoming);

            if (!PassTargetStateFilter(target, incoming.TargetStates))
                return new Result(ApplyMode.Reject, incoming);

            // Profile：不接受该类型直接拒绝（Boss 免疫挑空/击倒等）
            if (!profile.Accepts(incoming.ReactionType))
                return new Result(ApplyMode.Reject, incoming);

            // 归一化参数（避免外部传入脏数据）
            HitReactionRequest normalized = Normalize(incoming, in profile);

            // 优先级/互斥：默认“高优先级覆盖低优先级”
            int incomingPriority = profile.GetPriority(normalized.ReactionType);
            int currentPriority = GetPriority(target.CurrentState);

            // 当前没有受击：直接替换（启动）
            if (!target.IsInHitReaction)
                return new Result(ApplyMode.Replace, normalized);

            // 倒地/起身：默认只允许更高优先级覆盖（可后续通过配置表细化）
            if (incomingPriority > currentPriority)
                return new Result(ApplyMode.Replace, normalized);

            // 同级/低级：默认只刷新计时（避免频繁重播动画造成抖动）
            return new Result(profile.LowerOrEqualMode, normalized);
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
            if (profile.StunScale > 0f && profile.StunScale != 1f)
            {
                stun = Mathf.RoundToInt(stun * profile.StunScale);
            }
            if (profile.MaxHitStunMs > 0 && stun > profile.MaxHitStunMs)
            {
                stun = profile.MaxHitStunMs;
            }

            float kb = Mathf.Max(0f, r.KnockbackForce);
            if (profile.KnockbackScale > 0f && profile.KnockbackScale != 1f)
            {
                kb *= profile.KnockbackScale;
            }
            if (profile.MaxKnockbackForce > 0f && kb > profile.MaxKnockbackForce)
            {
                kb = profile.MaxKnockbackForce;
            }

            float ku = Mathf.Max(0f, r.KnockupForce);
            if (profile.KnockupScale > 0f && profile.KnockupScale != 1f)
            {
                ku *= profile.KnockupScale;
            }
            if (profile.MaxKnockupForce >= 0f && ku > profile.MaxKnockupForce)
            {
                ku = profile.MaxKnockupForce;
            }

            int hitStop = Mathf.Max(0, r.VictimHitStopMs);
            float shakeIntensity = Mathf.Max(0f, r.ScreenShakeIntensity);
            int shakeDurationMs = Mathf.Max(0, r.ScreenShakeDurationMs);
            float timeScale = r.TimeScale <= 0f ? 1f : r.TimeScale;
            int timeScaleMs = Mathf.Max(0, r.TimeScaleDurationMs);

            return new HitReactionRequest(
                r.ReactionType,
                r.TargetStates,
                dir,
                kb,
                ku,
                stun,
                hitStop,
                shakeIntensity,
                shakeDurationMs,
                timeScale,
                timeScaleMs);
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

        private static int GetPriority(HitState state)
        {
            // 与 HitReactionType 的默认映射保持一致
            switch (state)
            {
                case HitState.GetUp:
                    return 80;
                case HitState.Knockdown:
                    return 60;
                case HitState.Airborne:
                case HitState.Falling:
                    return 50;
                case HitState.Knockback:
                    return 40;
                case HitState.Stun:
                    return 20;
                default:
                    return 0;
            }
        }
    }
}

