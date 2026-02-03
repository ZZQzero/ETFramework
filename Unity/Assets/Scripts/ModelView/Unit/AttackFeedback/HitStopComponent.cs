using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// HitStop 对“运动域”的冻结策略（可配置）。
    /// 说明：
    /// - Animation 停顿由 Animancer.Graph.PauseGraph 负责（本组件默认都会暂停动画图）
    /// - Motion 冻结由 CharacterControllerComponentSystem 消费本枚举来执行
    /// </summary>
    public enum HitStopFreezeMode : byte
    {
        None = 0,                 // 不冻结运动（只停动画/只影响 combat-time）
        FreezeAnimationOnly = 1,  // 仅停动画（不冻结运动）
        FreezeXZOnly = 2,         // 冻结水平运动（允许 Y 重力/上抛继续）
        FreezeAll = 3,            // 冻结全部运动（XZ+Y 都停）
    }

    /// <summary>
    /// 战斗反馈控制器（客户端表现域）。
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
        /// 当前 HitStop 对“运动域”的冻结策略（多次 Request 会取更强的策略）。
        /// </summary>
        public HitStopFreezeMode FreezeMode { get; set; } = HitStopFreezeMode.FreezeAll;

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

        /// <summary>
        /// 上一次更新的帧号，用于确保在同一帧内多次访问 CombatTimeMs 时只更新一次。
        /// </summary>
        public int LastUpdateFrame { get; set; }

        /// <summary>
        /// 便于日志查看的快照字符串（调试用）。
        /// </summary>
        public override string ToString()
        {
            return $"顿帧组件(激活={this.IsHitStopActive}, 冻结模式={this.FreezeMode}, 结束实时时间={this.HitStopEndRealtimeMs}ms, 当前战斗时间={this.CombatTimeMs}ms, 本帧战斗增量={this.CombatDeltaMs}ms)";
        }
    }
}