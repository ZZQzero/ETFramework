using UnityEngine;

namespace ET
{
    /// <summary>
    /// 一次命中对“目标侧”的受击请求
    /// - Effect：影响目标的 gameplay effect（受击类型/控制/击退/击飞/硬直/过滤）
    /// - Feedback：命中反馈（受击者侧顿帧、以及可选的镜头/时间反馈参数）
    /// </summary>
    public readonly struct HitReactionRequest
    {
        /// <summary>视觉受击类型（决定规则层优先级/动画选择）。</summary>
        public readonly HitReactionType ReactionType;
        
        /// <summary>
        /// 攻击强度（用于目标侧规则判定）。数值越大越“强”。\n        /// </summary>
        public readonly byte HitStrength;

        /// <summary>物理运动数据（推/飞/砸/拉）。</summary>
        public readonly HitMotionData MotionData;
        /// <summary>目标状态过滤（可多选；Any=都可命中）。</summary>
        public readonly TargetStateMask TargetStates;

        /// <summary>命中方向（通常是 attacker→target 的水平向量）。</summary>
        public readonly Vector3 HitDirection;
        /// <summary>硬直时间(ms)（目标侧受击无法行动的持续时间）。</summary>
        public readonly int HitStunMs;

        // ===== 反馈（可选） =====
        /// <summary>受击者侧顿帧(ms)。</summary>
        public readonly int VictimHitStopMs;
        /// <summary>震屏强度(0-1)。</summary>
        public readonly float ScreenShakeIntensity;
        /// <summary>震屏时长(ms)。</summary>
        public readonly int ScreenShakeDurationMs;
        /// <summary>慢动作倍率（1=正常）。</summary>
        public readonly float TimeScale;
        /// <summary>慢动作持续时间(ms)。</summary>
        public readonly int TimeScaleDurationMs;

        /// <summary>
        /// 攻击方当前段的连击超时(ms)。用于受击方 AirCombo 续期对齐：EndCombatMs 至少延续到“攻击方下一段可命中的时间”，
        /// 避免“攻击动画还没结束（如 400ms）就因 MinAirTimeMs（如 260ms）触发 BeginExit”。
        /// 0 表示未设置，受击方仅用 profile.MinAirTimeMs。
        /// </summary>
        public readonly int AttackerSegmentComboTimeoutMs;

        /// <summary>
        /// 本次命中的攻击半径（来自 HitBox.Size，与 PhysicsHelper 判定一致）。用于空中连击水平距离限制与拉回。
        /// 0 表示未设置，受击方使用 profile.MaxAirHorizontalDistance。
        /// </summary>
        public readonly float AttackRadius;

        /// <summary>
        /// 攻击者世界坐标。用于空中连击拉回中心（拉回玩家附近）；(0,0,0) 表示未设置，受击方用受击者位置。
        /// </summary>
        public readonly Vector3 AttackerWorldPos;

        /// <summary>
        /// 完整构造（用于规则层 Normalize 后生成“有效请求”）。
        /// </summary>
        public HitReactionRequest(
            HitReactionType reactionType,
            byte hitStrength,
            HitMotionData motionData,
            TargetStateMask targetStates,
            Vector3 hitDirection,
            int hitStunMs,
            int victimHitStopMs = 0,
            float screenShakeIntensity = 0f,
            int screenShakeDurationMs = 0,
            float timeScale = 1f,
            int timeScaleDurationMs = 0,
            int attackerSegmentComboTimeoutMs = 0,
            float attackRadius = 0f,
            Vector3 attackerWorldPos = default)
        {
            this.ReactionType = reactionType;
            this.HitStrength = hitStrength;
            this.MotionData = motionData;
            this.TargetStates = targetStates;
            this.HitDirection = hitDirection;
            this.HitStunMs = hitStunMs;

            this.VictimHitStopMs = victimHitStopMs;
            this.ScreenShakeIntensity = screenShakeIntensity;
            this.ScreenShakeDurationMs = screenShakeDurationMs;
            this.TimeScale = timeScale;
            this.TimeScaleDurationMs = timeScaleDurationMs;
            this.AttackerSegmentComboTimeoutMs = attackerSegmentComboTimeoutMs;
            this.AttackRadius = attackRadius;
            this.AttackerWorldPos = attackerWorldPos;
        }

        public HitReactionRequest(in HitEffectData effect, in HitFeedbackData feedback, Vector3 hitDirection, int defaultHitStopMs, int attackerSegmentComboTimeoutMs = 0, float attackRadius = 0f, Vector3 attackerWorldPos = default)
        {
            ReactionType = effect.HitReaction;
            HitStrength = effect.HitStrength;
            MotionData = effect.HitMotion;
            TargetStates = effect.TargetStates;
            HitDirection = hitDirection;
            HitStunMs = effect.HitStunMs;
            VictimHitStopMs = feedback.ResolveVictimHitStopMs(defaultHitStopMs);
            ScreenShakeIntensity = feedback.ScreenShakeIntensity;
            ScreenShakeDurationMs = feedback.ScreenShakeDurationMs;
            TimeScale = feedback.TimeScale;
            TimeScaleDurationMs = feedback.TimeScaleDurationMs;
            AttackerSegmentComboTimeoutMs = attackerSegmentComboTimeoutMs;
            AttackRadius = attackRadius;
            AttackerWorldPos = attackerWorldPos;
        }


        public override string ToString()
        {
            return $"受击请求(类型={this.ReactionType}, Priority={this.HitStrength}, 运动={this.MotionData.MotionType}:强度{this.MotionData.Force}, 目标状态过滤={this.TargetStates}, 方向={this.HitDirection}, 硬直时长={this.HitStunMs}ms, 受击停顿={this.VictimHitStopMs}ms),攻击超时={this.AttackerSegmentComboTimeoutMs}ms";
        }

        /// <summary>
        /// 创建 Builder 用于链式构造 HitReactionRequest
        /// </summary>
        public static Builder Create() => new Builder();

        /// <summary>
        /// HitReactionRequest 的 Builder 类，提供链式 API 简化构造。
        /// </summary>
        public class Builder
        {
            private HitReactionType _reactionType = HitReactionType.None;
            private byte _hitStrength = 0;
            private HitMotionData _motionData = default;
            private TargetStateMask _targetStates = TargetStateMask.Any;
            private Vector3 _hitDirection = Vector3.zero;
            private int _hitStunMs = 0;
            private int _victimHitStopMs = 0;
            private float _screenShakeIntensity = 0f;
            private int _screenShakeDurationMs = 0;
            private float _timeScale = 1f;
            private int _timeScaleDurationMs = 0;
            private int _attackerSegmentComboTimeoutMs = 0;
            private float _attackRadius = 0f;
            private Vector3 _attackerWorldPos = default;

            /// <summary>设置受击类型和强度</summary>
            public Builder WithReaction(HitReactionType type, byte strength)
            {
                _reactionType = type;
                _hitStrength = strength;
                return this;
            }

            /// <summary>设置物理运动数据</summary>
            public Builder WithMotion(HitMotionData motion)
            {
                _motionData = motion;
                return this;
            }

            /// <summary>设置目标状态过滤</summary>
            public Builder WithTargetStates(TargetStateMask states)
            {
                _targetStates = states;
                return this;
            }

            /// <summary>设置命中方向</summary>
            public Builder WithDirection(Vector3 direction)
            {
                _hitDirection = direction;
                return this;
            }

            /// <summary>设置硬直时间</summary>
            public Builder WithStun(int stunMs)
            {
                _hitStunMs = stunMs;
                return this;
            }

            /// <summary>设置受击顿帧</summary>
            public Builder WithVictimHitStop(int hitStopMs)
            {
                _victimHitStopMs = hitStopMs;
                return this;
            }

            /// <summary>设置震屏效果</summary>
            public Builder WithScreenShake(float intensity, int durationMs)
            {
                _screenShakeIntensity = intensity;
                _screenShakeDurationMs = durationMs;
                return this;
            }

            /// <summary>设置时间缩放</summary>
            public Builder WithTimeScale(float scale, int durationMs)
            {
                _timeScale = scale;
                _timeScaleDurationMs = durationMs;
                return this;
            }

            /// <summary>设置攻击者连击超时</summary>
            public Builder WithAttackerComboTimeout(int timeoutMs)
            {
                _attackerSegmentComboTimeoutMs = timeoutMs;
                return this;
            }

            /// <summary>设置攻击半径</summary>
            public Builder WithAttackRadius(float radius)
            {
                _attackRadius = radius;
                return this;
            }

            /// <summary>设置攻击者位置</summary>
            public Builder WithAttackerPos(Vector3 worldPos)
            {
                _attackerWorldPos = worldPos;
                return this;
            }

            /// <summary>构建 HitReactionRequest</summary>
            public HitReactionRequest Build()
            {
                return new HitReactionRequest(
                    _reactionType,
                    _hitStrength,
                    _motionData,
                    _targetStates,
                    _hitDirection,
                    _hitStunMs,
                    _victimHitStopMs,
                    _screenShakeIntensity,
                    _screenShakeDurationMs,
                    _timeScale,
                    _timeScaleDurationMs,
                    _attackerSegmentComboTimeoutMs,
                    _attackRadius,
                    _attackerWorldPos
                );
            }
        }
    }
}
