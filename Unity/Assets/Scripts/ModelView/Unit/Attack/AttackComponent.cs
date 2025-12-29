using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

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
    /// 攻击组件 - 负责管理攻击连击逻辑
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AttackComponent : Entity, IAwake<string>, IDestroy, IUpdate
    {
        public AnimatorComponent AnimatorComponent { get; set; }
        #region 配置数据
        
        /// <summary>攻击配置资源路径</summary>
        public string ConfigPath { get; set; }
        
        /// <summary>攻击配置</summary>
        public AttackConfig Config { get; set; }
        
        #endregion

        #region 运行时状态
        
        /// <summary>当前攻击状态</summary>
        public AttackState State { get; set; } = AttackState.Idle;
        
        /// <summary>当前攻击段索引</summary>
        public int CurrentSegmentIndex { get; set; } = -1;
        
        /// <summary>当前攻击段数据</summary>
        public AttackSegmentData CurrentSegment { get; set; }
        
        /// <summary>当前动画状态</summary>
        public AnimancerState CurrentAnimState { get; set; }
        
        /// <summary>待播放的下一段攻击索引</summary>
        public int PendingSegmentIndex { get; set; } = -1;
        
        /// <summary>待播放的输入类型</summary>
        public ComboInputType PendingInputType { get; set; } = ComboInputType.None;
        
        /// <summary>连击计数</summary>
        public int ComboCount { get; set; }

        /// <summary>连击超时定时器ID</summary>
        public long ComboTimeoutTimer;
        
        /// <summary>顿帧结束时间</summary>
        public long HitStopEndTime { get; set; }
        
        /// <summary>顿帧前的动画速度</summary>
        public float HitStopPreviousSpeed { get; set; }
        
        #endregion

        #region 输入缓冲
        
        /// <summary>是否有缓冲输入</summary>
        public bool HasBufferedInput { get; set; }
        
        /// <summary>缓冲输入类型</summary>
        public ComboInputType BufferedInputType { get; set; } = ComboInputType.None;
        
        /// <summary>最后输入时间</summary>
        public long LastInputTime { get; set; }
        
        #endregion

        #region 命中检测
        
        /// <summary>当前攻击段是否已命中</summary>
        public bool HasHitThisSegment { get; set; }
        
        /// <summary>本次攻击已命中的目标ID集合</summary>
        public HashSet<GameObject> HitTargetsThisSegment { get; set; } = new HashSet<GameObject>();
        
        /// <summary>连击期间累计命中数</summary>
        public int TotalHitCount { get; set; }
        
        #endregion

        #region 位移控制
        
        /// <summary>位移开始位置</summary>
        public Vector3 MovementStartPosition { get; set; }
        
        /// <summary>位移目标位置</summary>
        public Vector3 MovementTargetPosition { get; set; }
        
        /// <summary>位移是否激活</summary>
        public bool IsMovementActive { get; set; }
        
        /// <summary>追踪目标</summary>
        public Transform TrackTarget { get; set; }

        public Transform Player;
        #endregion

        #region 便捷属性
        
        /// <summary>是否正在攻击</summary>
        public bool IsAttacking => State == AttackState.Attacking || State == AttackState.HitStop;
        
        /// <summary>是否在顿帧中</summary>
        public bool IsInHitStop => State == AttackState.HitStop;
        
        /// <summary>是否可以输入缓冲</summary>
        public bool CanBufferInput
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                    return false;
                return CurrentAnimState.NormalizedTime >= CurrentSegment.InputBufferStartTime;
            }
        }
        
        /// <summary>是否可以取消攻击</summary>
        public bool CanCancelAttack
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                    return false;
                return CurrentAnimState.NormalizedTime >= CurrentSegment.CancelableTime;
            }
        }
        
        /// <summary>当前动画归一化时间</summary>
        public float CurrentNormalizedTime
        {
            get
            {
                if (CurrentAnimState == null)
                    return 0f;
                return CurrentAnimState.NormalizedTime;
            }
        }
        
        #endregion

        #region 事件

        /// <summary>攻击开始事件</summary>
        public Action<int> OnAttackStart;
        
        /// <summary>攻击结束事件</summary>
        public Action<int> OnAttackEnd;
        
        /// <summary>命中事件</summary>
        public Action<GameObject, AttackSegmentData> OnHit;
        
        /// <summary>连击重置事件</summary>
        public Action OnComboReset;
        
        /// <summary>连击数变化事件</summary>
        public Action<int> OnComboCountChanged;
        
        #endregion
    }
}