using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 攻击配置资源（ScriptableObject包装器）
    /// </summary>
    [CreateAssetMenu(fileName = "AttackConfig", menuName = "ET/Combat/Attack Config")]
    public class AttackConfigAsset : ScriptableObject
    {
        [SerializeField]
        private AttackConfig _config = new AttackConfig();

        public AttackConfig Config => _config;

        #region Editor辅助方法
        
#if UNITY_EDITOR
        private int GetNextSegmentId()
        {
            // 不做旧资源兼容：约定配置资产内部数据永远有效。
            // 只保证新建段的 Id 不重复且稳定（删除中间段不会导致重排/复用 Id）。
            int maxId = -1;
            for (int i = 0; i < _config.Segments.Count; i++)
            {
                var seg = _config.Segments[i];
                if (seg != null && seg.Id > maxId)
                {
                    maxId = seg.Id;
                }
            }
            return maxId + 1;
        }

        /// <summary>
        /// 添加新的攻击段
        /// </summary>
        public AttackSegmentData AddSegment()
        {
            int id = this.GetNextSegmentId();
            var segment = new AttackSegmentData
            {
                Id = id,
                Name = $"Attack_{id}"
            };
            _config.Segments.Add(segment);
            return segment;
        }

        /// <summary>
        /// 移除攻击段
        /// </summary>
        public void RemoveSegment(int index)
        {
            if (index >= 0 && index < _config.Segments.Count)
            {
                int removedId = _config.Segments[index]?.Id ?? -1;
                _config.Segments.RemoveAt(index);

                // 商用级约束：Segment.Id 必须稳定，不能因为删除就整体重排
                // 否则 ComboBranches（按 Id 跳转）会被破坏。
                if (removedId >= 0)
                {
                    // 清理所有指向被删除段的分支引用，避免运行时跳转到不存在的段。
                    for (int i = 0; i < _config.Segments.Count; i++)
                    {
                        var seg = _config.Segments[i];
                        if (seg?.ComboBranches == null || seg.ComboBranches.Count == 0)
                        {
                            continue;
                        }

                        // 不能在 foreach 中直接 Remove，先收集 key 再删除。
                        List<ComboInputType> removeKeys = null;
                        foreach (var kv in seg.ComboBranches)
                        {
                            if (kv.Value == removedId)
                            {
                                removeKeys ??= new List<ComboInputType>();
                                removeKeys.Add(kv.Key);
                            }
                        }
                        if (removeKeys == null)
                        {
                            continue;
                        }
                        for (int r = 0; r < removeKeys.Count; r++)
                        {
                            seg.ComboBranches.Remove(removeKeys[r]);
                        }
                    }
                }
            }
        }
#endif
        
        #endregion
    }
}