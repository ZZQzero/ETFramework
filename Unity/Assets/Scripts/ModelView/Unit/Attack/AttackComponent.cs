using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 攻击组件 - 负责管理攻击连击逻辑
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AttackComponent : Entity, IAwake<string>, IDestroy, IUpdate, IFixedUpdate
    {
        public AnimatorComponent AnimatorComponent { get; set; }
        public CharacterControllerComponent CharacterController { get; set; }
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
        
        /// <summary>连击计数</summary>
        public int ComboCount { get; set; }

        /// <summary>连击超时定时器ID</summary>
        public long ComboTimeoutTimer;
        
        /// <summary>顿帧结束时间</summary>
        public long HitStopEndTime { get; set; }
        
        /// <summary>顿帧前的动画速度</summary>
        public float HitStopPreviousSpeed { get; set; }

        /// <summary>当前攻击段是否已经自然结束（避免 ExitAttackState 重复触发 OnAttackEnd）</summary>
        public bool CurrentSegmentEnded { get; set; }

        /// <summary>输入缓冲窗口是否已打开（由 AnimancerEvent 或轮询兜底驱动）</summary>
        public bool IsInputBufferWindowOpen { get; set; }

        /// <summary>取消窗口是否已打开（由 AnimancerEvent 或轮询兜底驱动）</summary>
        public bool IsCancelWindowOpen { get; set; }

        /// <summary>攻击期间是否锁定了角色移动</summary>
        public bool MovementLocked { get; set; }

        /// <summary>锁定移动前的 EnableMovement 状态</summary>
        public bool PrevEnableMovement { get; set; }
        
        #endregion

        #region 输入缓冲
        
        /// <summary>是否有缓冲输入</summary>
        public bool HasBufferedInput { get; set; }
        
        /// <summary>缓冲输入类型</summary>
        public ComboInputType BufferedInputType { get; set; } = ComboInputType.None;

        /// <summary>缓冲输入时间（用于 InputBufferWindowMs 过期）</summary>
        public long BufferedInputTime { get; set; }
        
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

        /// <summary>是否处于攻击流程中（包含后摇/顿帧），用于动画/移动系统判定</summary>
        public bool IsInAttack => State != AttackState.Idle;
        
        /// <summary>是否在顿帧中</summary>
        public bool IsInHitStop => State == AttackState.HitStop;
        
        /// <summary>是否可以输入缓冲</summary>
        public bool CanBufferInput
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                    return false;
                // 混合方案：
                // - 若 Animancer Events 已绑定，则以事件驱动的窗口标志为准（单一真相来源）
                // - 否则回退到 NormalizedTime 轮询兜底
                return CurrentAnimState.HasEvents
                    ? IsInputBufferWindowOpen
                    : CurrentAnimState.NormalizedTime >= CurrentSegment.TimeWindow.InputBufferStart;
            }
        }
        
        /// <summary>是否可以取消攻击</summary>
        public bool CanCancelAttack
        {
            get
            {
                if (CurrentAnimState == null || CurrentSegment == null)
                    return false;
                // 混合方案：
                // - 若 Animancer Events 已绑定，则以事件驱动的窗口标志为准（单一真相来源）
                // - 否则回退到 NormalizedTime 轮询兜底
                return CurrentAnimState.HasEvents
                    ? IsCancelWindowOpen
                    : CurrentAnimState.NormalizedTime >= CurrentSegment.TimeWindow.CancelableTime;
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