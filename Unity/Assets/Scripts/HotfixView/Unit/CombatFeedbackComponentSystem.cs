using System;
using Animancer;
using UnityEngine;

namespace ET
{
    [EntitySystemOf(typeof(CombatFeedbackComponent))]
    [FriendOf(typeof(CombatFeedbackComponent))]
    public static partial class CombatFeedbackComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CombatFeedbackComponent self)
        {
            long now = RealtimeMs();
            self.LastRealtimeMs = now;
            self.CombatTimeMs = 0;
            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;
            self.PausedAnimancers.Clear();
        }

        [EntitySystem]
        private static void Update(this CombatFeedbackComponent self)
        {
            long now = RealtimeMs();
            if (now < self.LastRealtimeMs)
            {
                // 极端情况（系统时间回拨/异常）：避免倒退导致负 delta
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
        private static void Destroy(this CombatFeedbackComponent self)
        {
            // 场景销毁时确保恢复（避免异常残留 speed=0）
            self.ForceEndHitStop();
            self.PausedAnimancers?.Clear();
        }

        /// <summary>
        /// 请求顿帧（HitStop）。
        /// - 自动合并：同一帧/多次请求会取 max(endTime)
        /// - 自动去重：同一 Animancer 只记录一次原 Speed，避免恢复错误
        /// </summary>
        public static void RequestHitStop(this CombatFeedbackComponent self, int durationMs, AnimancerComponent animancer)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (durationMs <= 0)
            {
                return;
            }

            long now = RealtimeMs();
            long end = now + durationMs;

            self.PauseAnimancer(animancer);

            if (!self.IsHitStopActive)
            {
                self.IsHitStopActive = true;
                self.HitStopEndRealtimeMs = end;
                Log.Error($"正常结束时间 {end}  {self.HitStopEndRealtimeMs - RealtimeMs()}");
            }
            else
            {
                // 叠加：延长结束时间（取 max）
                if (end > self.HitStopEndRealtimeMs)
                {
                    self.HitStopEndRealtimeMs = end;
                }
                Log.Error($"延长结束时间 {end}  {self.HitStopEndRealtimeMs - RealtimeMs()}");
            }
            Log.Error($"end {end}  {self.PausedAnimancers.Count}");
        }

        public static long NowCombatMs(this CombatFeedbackComponent self)
        {
            return self?.CombatTimeMs ?? 0;
        }

        private static void PauseAnimationTarget(this CombatFeedbackComponent self, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            // 只支持 Animancer：没有 AnimancerComponent 则不处理（保持职责单一、无兜底分支）
            var animancer = go.GetComponent<AnimancerComponent>();
            if (animancer != null)
            {
                self.PauseAnimancer(animancer);
            }
        }

        private static void PauseAnimancer(this CombatFeedbackComponent self, AnimancerComponent animancer)
        {
            if (animancer == null)
            {
                return;
            }

            var dict = self.PausedAnimancers;
            if (dict.ContainsKey(animancer))
            {
                return;
            }

            // 关键：不要改 Playable.Speed。
            // Animancer 推荐用 Graph.PauseGraph/UnpauseGraph 来冻结/恢复（底层是 PlayableGraph.Stop/Play）。
            // 同时记录“暂停前是否在播放”，避免恢复时强行把原本就暂停的图 Unpause。
            if (!animancer.IsGraphInitialized)
            {
                return;
            }

            AnimancerGraph graph = animancer.Graph;
            bool wasPlaying = graph.IsGraphPlaying;
            dict[animancer] = wasPlaying;
            graph.PauseGraph();
        }

        private static void EndHitStop(this CombatFeedbackComponent self)
        {
            if (!self.IsHitStopActive)
            {
                return;
            }

            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;

            // 恢复所有暂停的 Animancer
            Log.Info($"钝帧结束，恢复动画播放 {self.PausedAnimancers.Count}");
            var animancerDict = self.PausedAnimancers;
            if (animancerDict != null && animancerDict.Count > 0)
            {
                foreach (var kv in animancerDict)
                {
                    var animancer = kv.Key;
                    if (animancer == null)
                    {
                        continue;
                    }

                    if (!animancer.IsGraphInitialized)
                    {
                        continue;
                    }

                    // 只有之前在播放的图才恢复播放，避免干扰外部暂停状态。
                    if (kv.Value)
                    {
                        animancer.Graph.UnpauseGraph();
                    }
                }

                animancerDict.Clear();
            }

            // 相机冻结后续再做，这里先保持纯 HitStop 动画域控制。
        }

        private static void ForceEndHitStop(this CombatFeedbackComponent self)
        {
            // 无条件恢复（用于 Destroy/异常情况）
            self.IsHitStopActive = false;
            self.HitStopEndRealtimeMs = 0;

            var animancerDict = self.PausedAnimancers;
            if (animancerDict != null && animancerDict.Count > 0)
            {
                foreach (var kv in animancerDict)
                {
                    if (kv.Key != null)
                    {
                        if (kv.Key.IsGraphInitialized && kv.Value)
                        {
                            kv.Key.Graph.UnpauseGraph();
                        }
                    }
                }
                animancerDict.Clear();
            }

            // 相机冻结后续再做，这里先保持纯 HitStop 动画域控制。
        }

        private static long RealtimeMs()
        {
            return (long)(Time.realtimeSinceStartupAsDouble * 1000d);
        }
    }
}

