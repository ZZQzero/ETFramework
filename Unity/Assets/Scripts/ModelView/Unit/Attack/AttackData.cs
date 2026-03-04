using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET
{
    /// <summary>
    /// 攻击数据时间/数值工具：
    /// - 统一 Clamp/排序/比较阈值，避免运行时与编辑器各写一套导致语义漂移
    /// </summary>
    public static class AttackDataUtil
    {
        public const float Epsilon = 1e-6f;

        public static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        public static void Clamp01Range(ref float start, ref float end)
        {
            start = Clamp01(start);
            end = Clamp01(end);
            if (end < start)
            {
                (start, end) = (end, start);
            }
        }

        public static int ClampNonNegative(int v) => v < 0 ? 0 : v;
        public static float ClampNonNegative(float v) => v < 0f ? 0f : v;
    }

    /// <summary>
    /// 攻击状态枚举
    /// </summary>
    public enum AttackState
    {
        Idle = 0,           // 空闲
        Attacking = 1,      // 攻击中
        Recovery = 2,       // 后摇恢复中
    }
    /// <summary>
    /// 连击输入类型
    /// </summary>
    public enum ComboInputType
    {
        None = 0,
        Normal = 1,      // 普通攻击
        Heavy = 2,       // 重击
        Up = 3,          // 上挑
        Down = 4,        // 下砸
    }

    /// <summary>
    /// 攻击判定形状类型
    /// </summary>
    public enum HitShapeType
    {
        Box = 0,
        Sphere = 1,
        Fan = 2,         // 扇形
        Capsule = 3,
        Weapon = 4,//武器
    }

    /// <summary>
    /// 目标状态过滤（可多选）。
    /// - Any：地面/空中/倒地都命中
    /// </summary>
    [System.Flags]
    public enum TargetStateMask
    {
        Grounded = 1 << 0,
        Airborne = 1 << 1,
        Knockdown = 1 << 2,
        Any = Grounded | Airborne | Knockdown,
    }

    


    /// <summary>
    /// 受击物理运动类型（独立于表现类型）
    /// </summary>
    public enum HitMotionType : byte
    {
        None = 0,
        
        NormalHit,           //普通受击
        HorizontalImpulse,   // 纯水平冲量（XZ）
        UpwardImpulse,       // 向上冲量（可带XZ）
        DownwardImpulse,     // 向下冲量（砸地）
        TowardAttacker,      // 向攻击者中心拉
        CustomCurve,         // 曲线运动 / 特殊技能
    }

    /// <summary>
    /// 受击物理运动数据
    /// </summary>
    [Serializable]
    public struct HitMotionData
    {
        public HitMotionType MotionType;
        /// <summary>力度/速度</summary>
        public float Force;
        /// <summary>位移持续时间(ms)</summary>
        public int DurationMs;
        /// <summary>位移曲线（0-1 对应时间，Value 对应速度倍率）</summary>
        public AnimationCurve MotionCurve;
        
        public static HitMotionData Default => new HitMotionData
        {
            MotionType = HitMotionType.None,
            Force = 0f,
            DurationMs = 0,
            MotionCurve = AnimationCurve.Linear(0, 1, 1, 0) // 默认线性衰减
        };
    }

    /// <summary>
    /// 攻击段类型（用于连招逻辑判断）
    /// </summary>
    public enum SegmentType
    {
        Normal = 0,      // 普通攻击
        Heavy = 1,       // 重击
        Skill = 2,       // 技能段
        Finisher = 3,    // 收招/终结
        Launcher = 4,    // 上挑
        Slam = 5,        // 下砸
    }

    /// <summary>
    /// 时间窗口配置（归一化时间 0-1）
    /// </summary>
    [Serializable]
    public class TimeWindowData
    {
        /// <summary>
        /// 输入缓冲窗口开启点（归一化时间 0-1）。
        /// 说明：
        /// - 这是“何时开始允许缓冲/可衔接”的时间点，而不是“缓存能保留多久”。
        /// - 缓存输入的有效期（毫秒）由 <see cref="AttackConfig.InputBufferWindowMs"/> 控制。
        /// - 当前实现里会在该时间点把运行时标记 <c>IsInputBufferWindowOpen</c> 置为 true。
        /// </summary>
        public float InputBufferStart = 0.5f;
        
        /// <summary>
        /// 可取消窗口开启点（归一化时间 0-1）。
        /// 说明：用于限制“攻击过程中能否被技能/其他动作打断”的时间点。
        /// </summary>
        public float CancelableTime = 0.4f;
        
        /// <summary>
        /// 动画结束阈值（归一化时间 0-1）。
        /// 说明：用于提前判定“本段结束”，进入后摇/允许接段（不一定等到动画真正播放到 1.0）。
        /// </summary>
        public float AnimationEnd = 0.9f;

        /// <summary>
        /// 归一化/规范化（编辑器保存、配置构建时调用）。
        /// </summary>
        public void ValidateAndNormalize()
        {
            this.InputBufferStart = AttackDataUtil.Clamp01(this.InputBufferStart);
            this.CancelableTime = AttackDataUtil.Clamp01(this.CancelableTime);
            this.AnimationEnd = AttackDataUtil.Clamp01(this.AnimationEnd <= 0f ? 1f : this.AnimationEnd);

            // 约束：窗口点不应超过段结束阈值
            if (this.InputBufferStart > this.AnimationEnd) this.InputBufferStart = this.AnimationEnd;
            if (this.CancelableTime > this.AnimationEnd) this.CancelableTime = this.AnimationEnd;
        }

        /// <summary>
        /// 读取“已规范化”的输入缓冲窗口点（0~1）。
        /// 约束：在编辑器保存与运行时加载后都会调用 <see cref="ValidateAndNormalize"/>，因此运行时可完全信任该值已在 0~1 且 <= AnimationEnd。
        /// </summary>
        public float GetInputBufferStart01() => this.InputBufferStart;

        /// <summary>
        /// 读取“已规范化”的可取消窗口点（0~1）。
        /// </summary>
        public float GetCancelableTime01() => this.CancelableTime;

        /// <summary>
        /// 段结束阈值（0~1）。若配置<=0 则视为 1。
        /// </summary>
        public float GetAnimationEnd01() => this.AnimationEnd;
    }

    /// <summary>
    /// 击中效果数据（伤害、控制、状态过滤）
    /// </summary>
    [Serializable]
    public struct HitEffectData
    {
        /// <summary>伤害倍率</summary>
        public float DamageMultiplier;
        
        /// <summary>物理运动数据（位移/击飞）</summary>
        public HitMotionData HitMotion;

        /// <summary>硬直时间（毫秒）</summary>
        public int HitStunMs;

        /// <summary>
        /// 目标状态过滤（可多选）。
        /// </summary>
        public TargetStateMask TargetStates;

        public static HitEffectData Default => new HitEffectData
        {
            DamageMultiplier = 1f,
            HitMotion = HitMotionData.Default,
            HitStunMs = 200,
            TargetStates = TargetStateMask.Any,
        };
    }

    /// <summary>
    /// 击中反馈数据（顿帧、屏幕震动、时间缩放）
    /// </summary>
    [Serializable]
    public struct HitFeedbackData
    {
        /// <summary>
        /// 攻击者侧顿帧(ms)：
        /// - -1：使用 <see cref="AttackConfig.DefaultHitStopMs"/> 作为兜底
        /// -  0：不顿帧
        /// - >0：强制使用该值
        /// </summary>
        public int AttackerHitStopMs;

        /// <summary>
        /// 受击者侧顿帧(ms)：
        /// - -1：使用 <see cref="AttackConfig.DefaultHitStopMs"/> 作为兜底
        /// -  0：不顿帧（即使 profile 允许）
        /// - >0：强制使用该值（最终是否生效仍受 <see cref="HitFeedbackConfig.Options.AllowVictimHitStop"/> 控制）
        /// </summary>
        public int VictimHitStopMs;

        /// <summary>屏幕震动强度（0-1）</summary>
        public float ScreenShakeIntensity;

        /// <summary>屏幕震动时长（毫秒）</summary>
        public int ScreenShakeDurationMs;

        /// <summary>时间缩放（慢动作，1为正常）</summary>
        public float TimeScale;

        /// <summary>时间缩放持续时间（毫秒）</summary>
        public int TimeScaleDurationMs;

        public static HitFeedbackData Default => new HitFeedbackData
        {
            AttackerHitStopMs = -1,
            VictimHitStopMs = -1,
            ScreenShakeIntensity = 0f,
            ScreenShakeDurationMs = 0,
            TimeScale = 1f,
            TimeScaleDurationMs = 0,
        };

        public int ResolveAttackerHitStopMs(int defaultHitStopMs)
        {
            int ms = this.AttackerHitStopMs;
            if (ms < 0) ms = defaultHitStopMs;
            return ms < 0 ? 0 : ms;
        }

        public readonly int ResolveVictimHitStopMs(int defaultHitStopMs)
        {
            int ms = this.VictimHitStopMs;
            if (ms < 0) ms = defaultHitStopMs;
            return ms < 0 ? 0 : ms;
        }
    }

    /// <summary>
    /// 攻击判定数据（每个HitBox可独立配置效果）
    /// </summary>
    [Serializable]
    public class HitBoxData
    {
        public HitBoxData()
        {
            this.Effect = HitEffectData.Default;
            this.Feedback = HitFeedbackData.Default;
        }

        /// <summary>判定名称（编辑器显示用）</summary>
        public string Name = "HitBox";
        
        /// <summary>判定形状</summary>
        public HitShapeType ShapeType = HitShapeType.Box;
        
        /// <summary>相对角色的偏移</summary>
        public Vector3 Offset = new Vector3(0, 1f, 1f);

        /// <summary>
        /// 判定旋转（欧拉角，局部空间，度）。
        /// 说明：运行时会叠加到角色朝向上（worldRot = playerRot * Euler(RotationEuler)），用于更精细的挥砍/斜劈判定。
        /// </summary>
        public Vector3 RotationEuler = Vector3.zero;
        
        /// <summary>
        /// 尺寸：
        /// - Box: xyz
        /// - Sphere: x 为半径
        /// - Fan: x 为半径、y 为角度、z 为高度/厚度（可选，<=0 表示不限制高度）
        /// - Capsule: x 为半径、y 为高度（参见运行时 OverlapCapsule）
        /// </summary>
        public Vector3 Size = new Vector3(1f, 1f, 2f);

        /// <summary>
        /// 根据 <see cref="ShapeType"/> 从 Size 取“攻击半径”（与 PhysicsHelper 判定一致，用于空中连击水平距离限制等）。
        /// 四种类型语义不同，必须按类型取值：
        /// - Box: 半长轴最大值（halfExtents 最大值，包络球半径）
        /// - Sphere: Size.x 为半径
        /// - Fan: Size.x 为扇形半径
        /// - Capsule: Size.x 为胶囊半径
        /// </summary>
        public float GetAttackRadius()
        {
            switch (this.ShapeType)
            {
                case HitShapeType.Box:
                case HitShapeType.Weapon:
                    return Mathf.Max(this.Size.x, this.Size.y, this.Size.z) * 0.5f;
                case HitShapeType.Sphere:
                    return this.Size.x;
                case HitShapeType.Fan:
                    return this.Size.x;
                case HitShapeType.Capsule:
                    return this.Size.x;
                default:
                    return this.Size.x;
            }
        }

        /// <summary>判定开始时间（归一化 0-1）</summary>
        public float NormalizedStart = 0.2f;

        /// <summary>判定结束时间（归一化 0-1）</summary>
        public float NormalizedEnd = 0.5f;
        
        /// <summary>击中效果（伤害、控制）</summary>
        public HitEffectData Effect;

        /// <summary>击中反馈（顿帧、屏幕震动）</summary>
        public HitFeedbackData Feedback;

        /// <summary>
        /// 归一化/规范化（编辑器保存、配置构建时调用）。
        /// - segmentEnd01：段结束阈值（用于限制子事件不超过段结束语义）
        /// </summary>
        public void ValidateAndNormalize(float segmentEnd01)
        {
            float start = this.NormalizedStart;
            float end = this.NormalizedEnd;
            AttackDataUtil.Clamp01Range(ref start, ref end);
            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);

            if (start > segmentEnd01) start = segmentEnd01;
            if (end > segmentEnd01) end = segmentEnd01;
            if (end < start) end = start;

            this.NormalizedStart = start;
            this.NormalizedEnd = end;

            // ===== HitEffect 规范化（仅做数据合法化，不做“规则推导”）=====
            {
                HitEffectData e = this.Effect;
                e.HitStunMs = AttackDataUtil.ClampNonNegative(e.HitStunMs);
                e.DamageMultiplier = Mathf.Max(0f, e.DamageMultiplier);
                // 注意：Priority=0 表示未配置；默认值推导放在 HotfixView/Hotfix（避免 ModelView 承载规则逻辑）
                this.Effect = e;
            }
        }

        public void GetWindow01(float segmentEnd01, out float start, out float end)
        {
            start = this.NormalizedStart;
            end = this.NormalizedEnd;
            AttackDataUtil.Clamp01Range(ref start, ref end);
            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);

            if (start > segmentEnd01) start = segmentEnd01;
            if (end > segmentEnd01) end = segmentEnd01;
            if (end < start) end = start;
        }
    }

    /// <summary>
    /// 视觉特效数据
    /// </summary>
    [Serializable]
    public class VisualEffectData
    {
        /// <summary>特效名称</summary>
        public string Name = "New VFX";
        
        /// <summary>特效预制体</summary>
        public GameObject Prefab;
        
        /// <summary>开始时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedStart;
        
        /// <summary>
        /// 相对角色（Player Transform）的偏移量（局部空间）。
        /// 语义：
        /// - FollowTarget = true：特效作为 Player/挂点的子物体，直接使用 localPosition = Offset。
        /// - FollowTarget = false：特效生成时用 Player.TransformPoint(Offset) 计算一次世界坐标并“定格”，后续不再跟随角色移动。
        /// </summary>
        public Vector3 Offset;

        /// <summary>
        /// 相对角色（Player Transform）的旋转（局部欧拉角，度）。
        /// 语义：
        /// - FollowTarget = true：localRotation = Quaternion.Euler(RotationEuler)（随角色旋转一起转）。
        /// - FollowTarget = false：rotation = player.rotation * Quaternion.Euler(RotationEuler)（生成时烘焙为世界旋转并定格）。
        /// </summary>
        public Vector3 RotationEuler;

        /// <summary>
        /// 是否跟随目标（角色）的位置/旋转。
        /// - true：用于挥刀光效、身上常驻特效、武器 Trail 等（跟随角色/挂点）。
        /// - false：用于落点/地面AOE/法阵等（生成后定格在世界中，不随角色移动/旋转）。
        /// </summary>
        public bool FollowTarget = true;

        public bool IsAnimation = false;
        
        /// <summary>特效时长</summary>
        public float Length = 2f;

        public void ValidateAndNormalize(float segmentEnd01)
        {
            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);
            float t = AttackDataUtil.Clamp01(this.NormalizedStart);
            if (t > segmentEnd01) t = segmentEnd01;
            this.NormalizedStart = t;
            this.Length = Mathf.Max(0f, this.Length);
        }
    }

    /// <summary>
    /// 音效数据
    /// </summary>
    [Serializable]
    public class SoundEffectData
    {
        /// <summary>音效名称</summary>
        public string Name = "New SFX";
        
        /// <summary>音频剪辑</summary>
        public AudioClip Clip;
        
        /// <summary>开始时间（归一化 0-1）</summary>
        public float NormalizedStart;
        
        /// <summary>音量（0-1）</summary>
        public float Volume = 1f;

        public void ValidateAndNormalize(float segmentEnd01)
        {
            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);
            float t = AttackDataUtil.Clamp01(this.NormalizedStart);
            if (t > segmentEnd01) t = segmentEnd01;
            this.NormalizedStart = t;
            this.Volume = Mathf.Clamp01(this.Volume);
        }
    }

    /// <summary>
    /// 挂载对象 Active 控制数据（类似 Unity Timeline Activation Track）。
    /// 说明：
    /// - 用于控制“角色身上已经存在的对象”（常驻特效、武器Trail、碰撞体等）的显示/隐藏。
    /// - 不保存场景对象引用（避免 ScriptableObject 资产引用 SceneObject），只保存相对路径。
    /// - 播放语义：在 [NormalizedStart, NormalizedEnd] 区间内把目标对象设置为 <see cref="Active"/>。
    /// </summary>
    [Serializable]
    public class AttachedActiveData
    {
        /// <summary>显示名称（编辑器展示用）</summary>
        public string Name = "Active Toggle";

        /// <summary>
        /// 相对角色根节点（Unit/预览对象根 Transform）的路径。
        /// 例如："VFX/SlashTrail"。
        /// </summary>
        public string RelativePath = string.Empty;

        /// <summary>开始时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedStart = 0f;

        /// <summary>结束时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedEnd = 0.2f;

        public void ValidateAndNormalize(float segmentEnd01)
        {
            float start = this.NormalizedStart;
            float end = this.NormalizedEnd;
            AttackDataUtil.Clamp01Range(ref start, ref end);

            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);
            if (start > segmentEnd01) start = segmentEnd01;
            if (end > segmentEnd01) end = segmentEnd01;
            if (end < start) end = start;

            this.NormalizedStart = start;
            this.NormalizedEnd = end;
        }
    }
    
    /// <summary>
    /// 攻击移动数据
    /// </summary>
    [Serializable]
    public class AttackMovementData
    {
        /// <summary>是否启用位移</summary>
        public bool EnableMovement = false;
        
        /// <summary>位移距离</summary>
        public float Distance = 0f;
        
        /// <summary>位移开始时间（归一化 0-1,相对于动画片段）</summary>
        public float NormalizedStart = 0f;

        /// <summary>位移结束时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedEnd = 0.3f;
        
        /// <summary>位移曲线</summary>
        public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>是否追踪目标</summary>
        public bool TrackTarget = false;
        
        /// <summary>追踪范围</summary>
        public float TrackRange = 5f;

        /// <summary>
        /// 攻击中是否允许方向键微位移（Drift）。
        /// 启用后，Attacking 阶段按住方向键会叠加一个小幅度位移，用于追敌/微调站位。
        /// </summary>
        public bool AllowDrift = true;

        /// <summary>
        /// 微位移速度比例（相对于 MoveSpeed）。
        /// 例如 0.2 表示攻击中的移动速度为正常移速的 20%。
        /// </summary>
        public float DriftSpeedRatio = 0.2f;

        public void ValidateAndNormalize(float segmentEnd01)
        {
            this.Distance = Mathf.Max(0f, this.Distance);
            this.TrackRange = Mathf.Max(0f, this.TrackRange);
            this.DriftSpeedRatio = Mathf.Clamp01(this.DriftSpeedRatio);

            float start = this.NormalizedStart;
            float end = this.NormalizedEnd;
            AttackDataUtil.Clamp01Range(ref start, ref end);

            segmentEnd01 = AttackDataUtil.Clamp01(segmentEnd01 <= 0f ? 1f : segmentEnd01);
            if (start > segmentEnd01) start = segmentEnd01;
            if (end > segmentEnd01) end = segmentEnd01;
            if (end < start) end = start;

            this.NormalizedStart = start;
            this.NormalizedEnd = end;
        }
    }

    /// <summary>
    /// 攻击段数据（每个 Segment 对应一个动画片段）
    /// </summary>
    [Serializable]
    public class AttackSegmentData
    {
        // === 基础信息 ===
        /// <summary>攻击段ID</summary>
        public int Id;

        /// <summary>攻击段名称</summary>
        public string Name = string.Empty;

        /// <summary>攻击段类型</summary>
        public SegmentType Type = SegmentType.Normal;

        // === 动画 ===
        /// <summary>动画过渡配置</summary>
        public ClipTransition AnimationClipTrans;

        /// <summary>开始时间（秒，绝对时间）</summary>
        public float StartTime = 0f;
        /// <summary>
        /// 动画资源原始时长（秒，不受 Speed 影响）。用于在没有 AnimationClip 资源（例如服务端）时也能推导播放时长。
        /// </summary>
        public float ClipLength = 0f;
        /// <summary>
        /// 段持续时间（秒，绝对时间）。服务端/运行时用于把归一化子事件转换为绝对秒，不依赖 AnimationClip 资源。
        /// </summary>
        public float Duration = 0f;

        /// <summary>
        /// 连击超时偏移（毫秒）。
        /// 说明：用于计算“本段攻击流程的超时”，避免全局超时小于动画时长导致攻击还没播完就被强制退出。
        /// 计算公式：<c>SegmentTimeoutMs = max(0, Duration * AnimationEnd * 1000) + max(0, ComboTimeoutOffsetMs)</c>。
        /// </summary>
        public int ComboTimeoutOffsetMs = 200;

        // === 时间窗口 ===
        /// <summary>时间窗口配置</summary>
        public TimeWindowData TimeWindow = new TimeWindowData();

        // === 攻击判定 ===
        /// <summary>攻击判定列表（每个HitBox有独立的效果配置）</summary>
        public List<HitBoxData> HitBoxes = new List<HitBoxData>();
        
        // === 特效/音效 ===
        /// <summary>视觉特效列表</summary>
        public List<VisualEffectData> VisualEffects = new List<VisualEffectData>();
        
        /// <summary>音效列表</summary>
        public List<SoundEffectData> SoundEffects = new List<SoundEffectData>();

        /// <summary>
        /// 挂载对象 Active 控制列表（Activation Track）。
        /// 用于控制角色身上已存在对象的显隐/启用状态（不实例化）。
        /// </summary>
        public List<AttachedActiveData> AttachedActives = new List<AttachedActiveData>();
        
        // === 位移 ===
        /// <summary>攻击位移</summary>
        public AttackMovementData Movement = new AttackMovementData();
        
        // === 连招系统 ===
        /// <summary>可衔接的技能ID列表</summary>
        public List<int> CancelableSkillIds = new List<int>();
        
        /// <summary>连击分支（键为输入类型，值为下一段攻击ID）</summary>
        public Dictionary<ComboInputType, int> ComboBranches = new Dictionary<ComboInputType, int>();

        /// <summary>
        /// 获取 Segment 的有效播放时长（秒）：
        /// - 优先使用 Duration（用于服务端/无 Clip 情况）
        /// - 其次使用 AnimationClipTrans.Clip.length（编辑器/客户端有资源）
        /// - 再次使用 ClipLength（仅作为兜底展示字段）
        /// </summary>
        public float GetEffectiveDurationSec(float defaultDurationSec = 1f)
        {
            if (this.Duration > 0f)
            {
                return this.Duration;
            }

            if (this.AnimationClipTrans != null && this.AnimationClipTrans.Clip != null)
            {
                return this.AnimationClipTrans.Clip.length;
            }

            if (this.ClipLength > 0f)
            {
                return this.ClipLength;
            }

            return defaultDurationSec;
        }

        public float GetAnimationEnd01()
        {
            return this.TimeWindow != null ? this.TimeWindow.GetAnimationEnd01() : 1f;
        }

        public float ToAbsoluteTimeSec(float normalized01, float defaultDurationSec = 1f)
        {
            return this.StartTime + this.GetEffectiveDurationSec(defaultDurationSec) * AttackDataUtil.Clamp01(normalized01);
        }

        /// <summary>
        /// 归一化/规范化（编辑器保存、配置构建时调用）。
        /// - 目标：保证“窗口/子事件/位移”等都不越界，不产生隐性不一致。
        /// </summary>
        public void ValidateAndNormalize()
        {
            this.StartTime = Mathf.Max(0f, this.StartTime);
            this.ClipLength = Mathf.Max(0f, this.ClipLength);
            this.Duration = Mathf.Max(0f, this.Duration);
            this.ComboTimeoutOffsetMs = Mathf.Max(0, this.ComboTimeoutOffsetMs);

            this.TimeWindow ??= new TimeWindowData();
            this.TimeWindow.ValidateAndNormalize();

            float end01 = this.GetAnimationEnd01();

            this.Movement ??= new AttackMovementData();
            this.Movement.ValidateAndNormalize(end01);

            if (this.HitBoxes != null)
            {
                foreach (var hb in this.HitBoxes)
                {
                    hb?.ValidateAndNormalize(end01);
                }
            }

            if (this.VisualEffects != null)
            {
                foreach (var v in this.VisualEffects)
                {
                    v?.ValidateAndNormalize(end01);
                }
            }

            if (this.SoundEffects != null)
            {
                foreach (var s in this.SoundEffects)
                {
                    s?.ValidateAndNormalize(end01);
                }
            }

            if (this.AttachedActives != null)
            {
                foreach (var a in this.AttachedActives)
                {
                    a?.ValidateAndNormalize(end01);
                }
            }
        }
    }
}