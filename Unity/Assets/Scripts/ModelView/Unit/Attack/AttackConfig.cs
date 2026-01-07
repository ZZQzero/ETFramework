using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace ET
{
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
        
        /// <summary>
        /// 连击超时兜底（毫秒，兼容/兜底用）。
        /// 说明：
        /// - 方案A（分层）下推荐按“每段”计算超时：<c>段超时 = 段时长(ms) + 段偏移(ComboTimeoutOffsetMs)</c>。
        /// - 当段时长未知（例如配置缺失/异常）时，运行时会回退使用该值，防止卡死。
        /// </summary>
        public int ComboTimeoutMs = 800;
        
        /// <summary>
        /// 输入缓冲有效期（毫秒）。
        /// 说明：
        /// - 控制“缓存输入”在被消费前能保留多久，超过该时长会被判定为过期并清理。
        /// - 与 <see cref="TimeWindowData.InputBufferStart"/>（归一化时间点，窗口何时开启）是不同维度的参数：前者是毫秒有效期，后者是动画时间点。
        /// </summary>
        public int InputBufferWindowMs = 200;
        
        /// <summary>
        /// 默认顿帧时间（毫秒）。
        /// 说明：当某个 <see cref="HitBoxData"/> 未配置 <see cref="HitFeedbackData.HitStopMs"/>（<=0）时，运行时使用该值作为兜底顿帧时长。
        /// </summary>
        public int DefaultHitStopMs = 40;

        /// <summary>
        /// 攻击层（AttackLayer）在进入后摇(Recovery)后保持的时间（毫秒）。
        /// - 段结束进入 Recovery 且没有立刻接段时，不应立刻淡出 AttackLayer，否则会“闪回 Idle/Move”；
        ///   但也不能一直等到 <see cref="ComboTimeoutMs"/> 才淡出，否则玩家不输入时会长时间卡在攻击姿势/像没动画。
        /// - 该值用于在 Recovery 初期短暂保持攻击姿势，超时后自动淡出 AttackLayer 露出 Layer0。
        /// </summary>
        public int RecoveryHoldMs = 200;
        
        /// <summary>攻击段列表</summary>
        public List<AttackSegmentData> Segments = new List<AttackSegmentData>();

        [NonSerialized]
        private Dictionary<int, int> _idToIndex;

        [NonSerialized]
        private int _cachedSegmentCount = -1;

        private void EnsureIndexCache()
        {
            if (_idToIndex == null || _cachedSegmentCount != Segments.Count)
            {
                _cachedSegmentCount = Segments.Count;
                _idToIndex = new Dictionary<int, int>(Segments.Count);
                for (int i = 0; i < Segments.Count; i++)
                {
                    var seg = Segments[i];
                    if (seg == null)
                    {
                        continue;
                    }
                    _idToIndex[seg.Id] = i;
                }
            }
        }
        
        /// <summary>根据ID获取攻击段</summary>
        public AttackSegmentData GetSegmentById(int id)
        {
            int index = this.GetSegmentIndexById(id);
            return index >= 0 ? Segments[index] : null;
        }

        /// <summary>根据ID获取攻击段索引（不存在返回 -1）</summary>
        public int GetSegmentIndexById(int id)
        {
            this.EnsureIndexCache();
            return _idToIndex.TryGetValue(id, out int index) ? index : -1;
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
