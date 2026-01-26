using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 战斗反馈控制器（客户端表现域）。
    ///
    /// 设计目标（商业级手感）：
    /// - 命中反馈请求式（Request），同帧/多目标自动合并与叠加
    /// - HitStop 使用不受暂停影响的 realtime 计时
    /// - 暂停/恢复以“目标集合”为单位，避免重复写回导致恢复错误
    /// - 仅影响客户端表现（动画/相机），不影响网络/服务端逻辑
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class HitStopComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public AnimancerComponent Animancer { get; set; }
        /// <summary>
        /// 是否处于 HitStop（顿帧）中。
        /// </summary>
        public bool IsHitStopActive { get; set; }

        /// <summary>
        /// HitStop 结束的真实时间（毫秒，realtime）。
        /// </summary>
        public long HitStopEndRealtimeMs { get; set; }

        /// <summary>
        /// 战斗时间（毫秒，realtime 驱动，但在 HitStop 期间不推进）。
        /// 用于：输入缓冲有效期、连击超时、表现域计时等，避免“顿帧时逻辑偷偷流逝”的割裂手感。
        /// </summary>
        public long CombatTimeMs { get; set; }
        
        /// <summary>
        /// 本帧 combat-time 推进量（毫秒）。
        /// - 正常：等于本帧经过的真实时间（ms）
        /// - HitStop 期间：为 0（保证顿帧期间击退/浮空/硬直等不推进）
        /// </summary>
        public int CombatDeltaMs { get; set; }

        /// <summary>
        /// 上一次 Update 的 realtime 时间（毫秒）。
        /// </summary>
        public long LastRealtimeMs { get; set; }
    }
}