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
        HitStop = 3,        // 顿帧中
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
    }

    /// <summary>
    /// 目标状态类型
    /// </summary>
    public enum TargetStateType
    {
        Any = 0,         // 任意状态
        Grounded = 1,    // 地面
        Airborne = 2,    // 浮空
        Knockdown = 3,   // 倒地
    }

    /// <summary>
    /// 受击反应类型
    /// </summary>
    public enum HitReactionType
    {
        None = 0,
        Light = 1,       // 轻微受击
        Medium = 2,      // 中等受击
        Heavy = 3,       // 重度受击
        Knockback = 4,   // 击退
        Knockup = 5,     // 击飞
        Knockdown = 6,   // 击倒
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
    }

    /// <summary>
    /// 击中效果数据（伤害、控制、状态过滤）
    /// </summary>
    [Serializable]
    public class HitEffectData
    {
        /// <summary>伤害倍率</summary>
        public float DamageMultiplier = 1f;
        
        /// <summary>受击反应类型</summary>
        public HitReactionType HitReaction = HitReactionType.Light;
        
        /// <summary>击退力度</summary>
        public float KnockbackForce = 0f;
        
        /// <summary>击飞力度</summary>
        public float KnockupForce = 0f;
        
        /// <summary>硬直时间（毫秒）</summary>
        public int HitStunMs = 200;
        
        /// <summary>目标状态过滤</summary>
        public TargetStateType TargetState = TargetStateType.Any;
    }

    /// <summary>
    /// 击中反馈数据（顿帧、屏幕震动、时间缩放）
    /// </summary>
    [Serializable]
    public class HitFeedbackData
    {
        /// <summary>屏幕震动强度（0-1）</summary>
        public float ScreenShakeIntensity = 0f;
        
        /// <summary>屏幕震动时长（秒）</summary>
        public float ScreenShakeDuration = 0f;
        
        /// <summary>
        /// 顿帧时长（毫秒）。
        /// 说明：<= 0 表示不在该 HitBox 上强制顿帧，运行时会回退使用 <see cref="AttackConfig.DefaultHitStopMs"/>。
        /// </summary>
        public int HitStopMs = 0;
        
        /// <summary>时间缩放（慢动作，1为正常）</summary>
        public float TimeScale = 1f;
        
        /// <summary>时间缩放持续时间（毫秒）</summary>
        public int TimeScaleDurationMs = 0;
    }

    /// <summary>
    /// 攻击判定数据（每个HitBox可独立配置效果）
    /// </summary>
    [Serializable]
    public class HitBoxData
    {
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
        /// - Fan: x 为半径、y 为角度、z 为高度/厚度（可选，<=0 表示不限制高度，兼容旧行为）
        /// - Capsule: x 为半径、y 为高度（参见运行时 OverlapCapsule）
        /// </summary>
        public Vector3 Size = new Vector3(1f, 1f, 2f);
        
        /// <summary>判定开始时间（归一化 0-1）</summary>
        public float NormalizedStart = 0.2f;

        /// <summary>判定结束时间（归一化 0-1）</summary>
        public float NormalizedEnd = 0.5f;
        
        /// <summary>击中效果（伤害、控制）</summary>
        public HitEffectData Effect = new HitEffectData();
        
        /// <summary>击中反馈（顿帧、屏幕震动）</summary>
        public HitFeedbackData Feedback = new HitFeedbackData();
        
        // === 运行时状态 ===
        /// <summary>是否已激活判定</summary>
        [NonSerialized]
        public bool IsActive;
        
        /// <summary>是否已完成判定</summary>
        [NonSerialized]
        public bool IsCompleted;
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
        
        /// <summary>相对角色的偏移量</summary>
        public Vector3 Offset;
        
        /// <summary>是否跟随目标移动</summary>
        public bool FollowTarget;
        
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
        [FormerlySerializedAs("StartTime")]
        public float NormalizedStart = 0f;

        /// <summary>位移结束时间（归一化 0-1，相对于动画片段）</summary>
        [FormerlySerializedAs("EndTime")]
        public float NormalizedEnd = 0.3f;
        
        /// <summary>位移曲线</summary>
        public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>是否追踪目标</summary>
        public bool TrackTarget = false;
        
        /// <summary>追踪范围</summary>
        public float TrackRange = 5f;
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
        /// 计算公式：<c>SegmentTimeoutMs = max(0, Duration * 1000) + max(0, ComboTimeoutOffsetMs)</c>。
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