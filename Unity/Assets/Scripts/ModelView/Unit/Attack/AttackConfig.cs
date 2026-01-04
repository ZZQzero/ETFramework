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
        
        /// <summary>连击超时时间（毫秒）</summary>
        public int ComboTimeoutMs = 800;
        
        /// <summary>输入缓冲窗口时间（毫秒）</summary>
        public int InputBufferWindowMs = 200;
        
        /// <summary>默认顿帧时间（毫秒）</summary>
        public int DefaultHitStopMs = 40;
        
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
