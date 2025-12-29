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
        /// <summary>
        /// 添加新的攻击段
        /// </summary>
        public AttackSegmentData AddSegment()
        {
            var segment = new AttackSegmentData
            {
                Id = _config.Segments.Count,
                Name = $"Attack_{_config.Segments.Count + 1}"
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
                _config.Segments.RemoveAt(index);
                // 重新分配ID
                for (int i = 0; i < _config.Segments.Count; i++)
                {
                    _config.Segments[i].Id = i;
                }
            }
        }
#endif
        
        #endregion
    }
}