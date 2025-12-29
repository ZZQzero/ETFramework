using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
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
    /// 攻击判定数据
    /// </summary>
    [Serializable]
    public class HitBoxData
    {
        /// <summary>判定形状</summary>
        public HitShapeType ShapeType = HitShapeType.Box;
        
        /// <summary>相对角色的偏移</summary>
        public Vector3 Offset = new Vector3(0, 1f, 1f);
        
        /// <summary>尺寸（Box: xyz, Sphere: x为半径, Fan: x为半径y为角度）</summary>
        public Vector3 Size = new Vector3(1f, 1f, 2f);
        
        /// <summary>判定开始时间（NormalizedTime 0-1）</summary>
        public float StartTime = 0.2f;
        
        /// <summary>判定结束时间（NormalizedTime 0-1）</summary>
        public float EndTime = 0.5f;
        
        /// <summary>是否已激活判定</summary>
        [NonSerialized]
        public bool IsActive;
        
        /// <summary>是否已完成判定</summary>
        [NonSerialized]
        public bool IsCompleted;
    }

    /// <summary>
    /// 攻击效果数据
    /// </summary>
    [Serializable]
    public class AttackEffectData
    {
        /// <summary>攻击特效路径</summary>
        public string AttackEffectPath = string.Empty;
        
        /// <summary>命中特效路径</summary>
        public string HitEffectPath = string.Empty;
        
        /// <summary>攻击音效路径</summary>
        public string AttackSoundPath = string.Empty;
        
        /// <summary>命中音效路径</summary>
        public string HitSoundPath = string.Empty;
        
        /// <summary>屏幕震动强度（0-1）</summary>
        public float ScreenShakeIntensity = 0f;
        
        /// <summary>屏幕震动时长（秒）</summary>
        public float ScreenShakeDuration = 0f;
        
        /// <summary>顿帧时长（毫秒）</summary>
        public int HitStopMs = 0;
        
        /// <summary>时间缩放（慢动作效果，1为正常）</summary>
        public float TimeScale = 1f;
        
        /// <summary>时间缩放持续时间（毫秒）</summary>
        public int TimeScaleDurationMs = 0;
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
        
        /// <summary>位移开始时间（NormalizedTime）</summary>
        public float StartTime = 0f;
        
        /// <summary>位移结束时间（NormalizedTime）</summary>
        public float EndTime = 0.3f;
        
        /// <summary>位移曲线</summary>
        public AnimationCurve MoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>是否追踪目标</summary>
        public bool TrackTarget = false;
        
        /// <summary>追踪范围</summary>
        public float TrackRange = 5f;
    }

    /// <summary>
    /// 攻击段数据
    /// </summary>
    [Serializable]
    public class AttackSegmentData
    {
        /// <summary>攻击段ID</summary>
        public int Id;
        
        /// <summary>攻击段名称</summary>
        public string Name = string.Empty;
        
        /// <summary>动画资源路径</summary>
        public string AnimationPath = string.Empty;
        
        /// <summary>动画过渡时间（秒）</summary>
        public float FadeDuration = 0.1f;
        
        /// <summary>动画播放速度</summary>
        public float AnimationSpeed = 1f;
        
        /// <summary>基础伤害倍率</summary>
        public float DamageMultiplier = 1f;
        
        /// <summary>受击反应类型</summary>
        public HitReactionType HitReaction = HitReactionType.Light;
        
        /// <summary>击退力度</summary>
        public float KnockbackForce = 0f;
        
        /// <summary>击飞力度</summary>
        public float KnockupForce = 0f;
        
        /// <summary>硬直时间（毫秒）</summary>
        public int HitStunMs = 200;
        
        /// <summary>输入缓冲开始时间（NormalizedTime 0-1）</summary>
        public float InputBufferStartTime = 0.5f;
        
        /// <summary>可取消时间点（NormalizedTime 0-1）</summary>
        public float CancelableTime = 0.4f;
        
        /// <summary>动画结束时间点（NormalizedTime 0-1）</summary>
        public float EndTime = 0.9f;
        
        /// <summary>适用的目标状态</summary>
        public TargetStateType TargetState = TargetStateType.Any;
        
        /// <summary>攻击判定列表（支持多段判定）</summary>
        public List<HitBoxData> HitBoxes = new List<HitBoxData>();
        
        /// <summary>攻击效果</summary>
        public AttackEffectData Effect = new AttackEffectData();
        
        /// <summary>攻击位移</summary>
        public AttackMovementData Movement = new AttackMovementData();
        
        /// <summary>可衔接的技能ID列表</summary>
        public List<int> CancelableSkillIds = new List<int>();
        
        /// <summary>连击分支（键为输入类型，值为下一段攻击ID）</summary>
        public Dictionary<ComboInputType, int> ComboBranches = new Dictionary<ComboInputType, int>();
        
        /// <summary>运行时：动画过渡引用</summary>
        [NonSerialized]
        public ITransition Transition;
        
        /// <summary>运行时：是否已加载</summary>
        [NonSerialized]
        public bool IsLoaded;
    }

    /// <summary>
    /// 攻击配置资源
    /// </summary>
    [Serializable]
    public class AttackConfig
    {
        /// <summary>配置ID</summary>
        public int ConfigId;
        
        /// <summary>配置名称</summary>
        public string ConfigName = string.Empty;
        
        /// <summary>连击超时时间（毫秒）</summary>
        public int ComboTimeoutMs = 800;
        
        /// <summary>输入缓冲窗口时间（毫秒）</summary>
        public int InputBufferWindowMs = 200;
        
        /// <summary>默认顿帧时间（毫秒）</summary>
        public int DefaultHitStopMs = 40;
        
        /// <summary>攻击段列表</summary>
        public List<AttackSegmentData> Segments = new List<AttackSegmentData>();
        
        /// <summary>根据ID获取攻击段</summary>
        public AttackSegmentData GetSegmentById(int id)
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].Id == id)
                {
                    return Segments[i];
                }
            }
            return null;
        }
        
        /// <summary>根据索引获取攻击段</summary>
        public AttackSegmentData GetSegmentByIndex(int index)
        {
            if (index >= 0 && index < Segments.Count)
            {
                return Segments[index];
            }
            return null;
        }
    }
}