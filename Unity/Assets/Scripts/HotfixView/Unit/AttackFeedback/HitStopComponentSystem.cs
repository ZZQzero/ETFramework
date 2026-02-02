using System;
using Animancer;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(HitStopComponent))]
    [FriendOf(typeof(HitStopComponent))]
    public static partial class HitStopComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HitStopComponent self)
        {
            self.LastRealtimeMs = 0;
            self.CombatTimeMs = 0;
            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;
            self.FreezeMode = HitStopFreezeMode.FreezeAll;
        }

        [EntitySystem]
        private static void Update(this HitStopComponent self)
        {
            long now = RealtimeMs();
            
            if (self.LastRealtimeMs <= 0 || now < self.LastRealtimeMs)
            {
                self.LastRealtimeMs = now;
                self.CombatDeltaMs = 0;
                return;
            }
            
            long prevCombat = self.CombatTimeMs;

            // 计算 combat-time：HitStop 期间不推进，结束后继续推进
            if (!self.IsHitStopActive)
            {
                self.CombatTimeMs += now - self.LastRealtimeMs;
            }
            else
            {
                // HitStop 期间不推进 combat time；若本帧跨过结束点，则把“结束点之后的时间”补进 combat time
                if (now >= self.HitStopEndRealtimeMs)
                {
                    long after = now - Math.Max(self.LastRealtimeMs, self.HitStopEndRealtimeMs);
                    if (after > 0)
                    {
                        self.CombatTimeMs += after;
                    }

                    self.EndHitStop();
                }
            }

            long delta = self.CombatTimeMs - prevCombat;
            if (delta < 0) delta = 0;
            self.CombatDeltaMs = delta > int.MaxValue ? int.MaxValue : (int)delta;
            self.LastRealtimeMs = now;
        }

        [EntitySystem]
        private static void Destroy(this HitStopComponent self)
        {
            // 场景销毁时确保恢复（避免异常残留 speed=0）
            self.ForceEndHitStop();
        }

        /// <summary>
        /// 请求顿帧（HitStop）并指定“运动域冻结策略”。
        /// - 时长叠加：取更晚结束点
        /// - 冻结策略叠加：取更强策略（None < AnimationOnly < FreezeXZOnly < FreezeAll）
        /// </summary>
        public static void RequestHitStop(this HitStopComponent self, int durationMs, AnimancerComponent animancer, HitStopFreezeMode freezeMode)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (durationMs <= 0)
            {
                return;
            }

            self.Animancer = animancer;
            long now = RealtimeMs();
            long end = now + durationMs;
            self.PauseAnimancer();

            if (!self.IsHitStopActive)
            {
                self.IsHitStopActive = true;
                self.HitStopEndRealtimeMs = end;
                self.FreezeMode = freezeMode;
            }
            else
            {
                // 叠加：延长结束时间（取 max）
                if (end > self.HitStopEndRealtimeMs)
                {
                    self.HitStopEndRealtimeMs = end;
                }

                // 叠加：冻结策略取更强
                if (freezeMode > self.FreezeMode)
                {
                    self.FreezeMode = freezeMode;
                }
            }
        }

        private static void PauseAnimancer(this HitStopComponent self)
        {
            if (self.Animancer == null || !self.Animancer.IsGraphInitialized)
            {
                return;
            }

            AnimancerGraph graph = self.Animancer.Graph;
            graph.PauseGraph();
        }

        private static void EndHitStop(this HitStopComponent self)
        {
            if (!self.IsHitStopActive)
            {
                return;
            }

            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;
            self.FreezeMode = HitStopFreezeMode.FreezeAll;

            if (self.Animancer != null)
            {
                self.Animancer.Graph.UnpauseGraph();
            }
        }

        private static void ForceEndHitStop(this HitStopComponent self)
        {
            // 无条件恢复（用于 Destroy/异常情况）
            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;
            self.FreezeMode = HitStopFreezeMode.FreezeAll;
            if (self.Animancer != null)
            {
                self.Animancer.Graph.UnpauseGraph();
            }
        }

        public static long NowCombatMs(this HitStopComponent self)
        {
            return self?.CombatTimeMs ?? 0;
        }
        private static long RealtimeMs()
        {
            return (long)(Time.realtimeSinceStartupAsDouble * 1000d);
        }
    }
}

