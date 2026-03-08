using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

namespace ET
{
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
        /// </summary>
        public float InputBufferStart = 0.5f;

        /// <summary>
        /// 可取消窗口开启点（归一化时间 0-1）。
        /// </summary>
        public float CancelableTime = 0.4f;

        /// <summary>
        /// 动画结束阈值（归一化时间 0-1）。
        /// </summary>
        public float AnimationEnd = 0.9f;
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
        /// <summary>攻击者侧顿帧(ms)，0=不顿帧。</summary>
        public int AttackerHitStopMs;

        /// <summary>受击者侧顿帧(ms)，0=不顿帧（最终是否生效仍受 Victim Profile 控制）。</summary>
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
            AttackerHitStopMs = 0,
            VictimHitStopMs = 0,
            ScreenShakeIntensity = 0f,
            ScreenShakeDurationMs = 0,
            TimeScale = 1f,
            TimeScaleDurationMs = 0,
        };
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
        /// 根据 <see cref="ShapeType"/> 从 Size 取"攻击半径"。
        /// </summary>
        public float GetAttackRadius()
        {
            switch (this.ShapeType)
            {
                case HitShapeType.Box:
                case HitShapeType.Weapon:
                    return Mathf.Max(this.Size.x, this.Size.y, this.Size.z) * 0.5f;
                case HitShapeType.Sphere:
                case HitShapeType.Fan:
                case HitShapeType.Capsule:
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
        /// </summary>
        public Vector3 Offset;

        /// <summary>
        /// 相对角色（Player Transform）的旋转（局部欧拉角，度）。
        /// </summary>
        public Vector3 RotationEuler;

        /// <summary>
        /// 是否跟随目标（角色）的位置/旋转。
        /// </summary>
        public bool FollowTarget = true;

        public bool IsAnimation = false;

        /// <summary>特效时长</summary>
        public float Length = 2f;
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
    }

    /// <summary>
    /// 挂载对象 Active 控制数据（类似 Unity Timeline Activation Track）。
    /// </summary>
    [Serializable]
    public class AttachedActiveData
    {
        /// <summary>显示名称（编辑器展示用）</summary>
        public string Name = "Active Toggle";

        /// <summary>
        /// 相对角色根节点的路径。例如："VFX/SlashTrail"。
        /// </summary>
        public string RelativePath = string.Empty;

        /// <summary>开始时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedStart = 0f;

        /// <summary>结束时间（归一化 0-1，相对于动画片段）</summary>
        public float NormalizedEnd = 0.2f;
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
        /// </summary>
        public bool AllowDrift = true;

        /// <summary>
        /// 微位移速度比例（相对于 MoveSpeed）。
        /// </summary>
        public float DriftSpeedRatio = 0.2f;
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
        /// 动画资源原始时长（秒，不受 Speed 影响）。
        /// </summary>
        public float ClipLength = 0f;
        /// <summary>
        /// 段持续时间（秒，绝对时间）。
        /// </summary>
        public float Duration = 0f;

        /// <summary>
        /// 连击超时偏移（毫秒）。
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

    }
}
