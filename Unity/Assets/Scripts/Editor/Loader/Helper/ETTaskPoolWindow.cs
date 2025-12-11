#if ENABLE_VIEW
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// ETTask对象池可视化监控窗口
    /// 参考GameObjectPoolWindow的设计
    /// </summary>
    public class ETTaskPoolWindow : EditorWindow
    {
        private static ETTaskPoolWindow _window;
        private Vector2 _scrollPos;
        
        // GUIStyle
        private GUIStyle _titleStyle;
        private GUIStyle _normalStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _goodStyle;
        private GUIStyle _separatorStyle;
        
        // 统计数据
        private ETTaskPoolStats _stats = new ETTaskPoolStats();
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f; // 0.5秒更新一次
        
        // 扫描状态
        private bool _isScanning = false;
        
        [MenuItem("ET/View/ETTask对象池监控")]
        public static void ShowWindow()
        {
            _window = GetWindow<ETTaskPoolWindow>("ETTask对象池");
            _window.minSize = new Vector2(600, 400);
            _window.Show();
        }
        
        private void OnEnable()
        {
            InitStyles();
        }
        
        private void InitStyles()
        {
            _titleStyle = new GUIStyle();
            _titleStyle.fontSize = 14;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.yellow;
            
            _normalStyle = new GUIStyle();
            _normalStyle.normal.textColor = Color.white;
            
            _warningStyle = new GUIStyle();
            _warningStyle.normal.textColor = Color.red;
            
            _goodStyle = new GUIStyle();
            _goodStyle.normal.textColor = Color.green;
            
            _separatorStyle = new GUIStyle();
            _separatorStyle.normal.textColor = Color.gray;
        }
        
        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            if (Time.realtimeSinceStartup - _lastUpdateTime > UPDATE_INTERVAL)
            {
                _lastUpdateTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }
        
        private void OnGUI()
        {
            if (_titleStyle == null)
            {
                InitStyles();
            }
            
            if (Application.isPlaying)
            {
                UpdateStats();
            }
            
            EditorGUILayout.Space(10);
            
            // 标题
            EditorGUILayout.LabelField("ETTask对象池实时监控", _titleStyle);
            EditorGUILayout.Space(5);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            // 总览
            DrawOverview();
            
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);
            
            // ETTask池详情
            DrawETTaskPoolDetail();
            
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);
            
            // ETTask<T>池详情（如果有的话）
            DrawGenericPoolsDetail();
            
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(10);
            
            // 健康检查
            DrawHealthCheck();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            DrawButtons();
        }
        
        private void UpdateStats()
        {
            _stats.UpdateFromRuntime();
        }
        
        private void DrawOverview()
        {
            EditorGUILayout.LabelField("📊 总览", _titleStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"运行状态：{(Application.isPlaying ? "运行中" : "未运行")}", 
                Application.isPlaying ? _goodStyle : _warningStyle);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("请在Play模式下查看对象池统计信息", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"ETTask池总数：{_stats.PoolCount}", _normalStyle);
            EditorGUILayout.LabelField($"总对象数：{_stats.TotalObjects}", _normalStyle);
        }
        
        private void DrawETTaskPoolDetail()
        {
            EditorGUILayout.LabelField("🔷 ETTask 对象池", _titleStyle);
            EditorGUILayout.Space(5);
            
            if (!Application.isPlaying)
            {
                return;
            }
            
            var info = _stats.ETTaskPoolInfo;
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"池大小：{info.PoolSize} / {info.MaxSize}", _normalStyle);
            
            // 进度条
            float fillRate = info.MaxSize > 0 ? (float)info.PoolSize / info.MaxSize : 0;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), fillRate, 
                $"{fillRate * 100:F1}% 满");
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField($"总分配次数：{info.TotalAllocations:N0}", _normalStyle);
            EditorGUILayout.LabelField($"池命中次数：{info.PoolHits:N0}", _goodStyle);
            EditorGUILayout.LabelField($"池未命中次数：{info.PoolMisses:N0}", 
                info.PoolMisses > info.PoolHits ? _warningStyle : _normalStyle);
            EditorGUILayout.LabelField($"重复归还次数：{info.DuplicateReturns:N0}", 
                info.DuplicateReturns > 0 ? _warningStyle : _goodStyle);
            
            EditorGUILayout.Space(5);
            
            float hitRate = info.TotalAllocations > 0 
                ? (float)info.PoolHits / info.TotalAllocations * 100 
                : 0;
            
            GUIStyle hitRateStyle = hitRate > 90 ? _goodStyle : 
                                    hitRate > 70 ? _normalStyle : _warningStyle;
            EditorGUILayout.LabelField($"命中率：{hitRate:F2}%", hitRateStyle);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGenericPoolsDetail()
        {
            EditorGUILayout.LabelField("🔶 ETTask<T> 泛型对象池", _titleStyle);
            EditorGUILayout.Space(5);
            
            if (!Application.isPlaying)
            {
                return;
            }
            
            if (_stats.GenericPoolInfos.Count == 0)
            {
                EditorGUILayout.LabelField("正在扫描程序集，请稍候...", _normalStyle);
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("首次打开窗口时会扫描程序集以发现ETTask<T>的使用，这可能需要几秒钟。", MessageType.Info);
                
                if (!_isScanning)
                {
                    _isScanning = true;
                    EditorApplication.delayCall += () =>
                    {
                        _isScanning = false;
                        UpdateStats();
                        Repaint();
                    };
                }
                return;
            }
            
            foreach (var kvp in _stats.GenericPoolInfos)
            {
                string typeName = kvp.Key;
                var info = kvp.Value;
                
                EditorGUILayout.BeginVertical("box");
                
                // 简化类型名称显示（去掉命名空间前缀）
                string displayName = typeName;
                int lastDot = typeName.LastIndexOf('.');
                if (lastDot >= 0)
                {
                    displayName = typeName.Substring(lastDot + 1);
                }
                
                EditorGUILayout.LabelField($"类型：ETTask<{displayName}>", _normalStyle);
                EditorGUILayout.LabelField($"完整类型：{typeName}", _separatorStyle);
                
                EditorGUILayout.Space(3);
                
                EditorGUILayout.LabelField($"池大小：{info.PoolSize} / {info.MaxSize}", _normalStyle);
                
                float fillRate = info.MaxSize > 0 ? (float)info.PoolSize / info.MaxSize : 0;
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), fillRate, 
                    $"{fillRate * 100:F1}% 满");
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField($"总分配次数：{info.TotalAllocations:N0}", _normalStyle);
                EditorGUILayout.LabelField($"池命中次数：{info.PoolHits:N0}", _goodStyle);
                EditorGUILayout.LabelField($"池未命中次数：{info.PoolMisses:N0}", 
                    info.PoolMisses > info.PoolHits ? _warningStyle : _normalStyle);
                EditorGUILayout.LabelField($"重复归还次数：{info.DuplicateReturns:N0}", 
                    info.DuplicateReturns > 0 ? _warningStyle : _goodStyle);
                
                EditorGUILayout.Space(5);
                
                // 命中率
                GUIStyle hitRateStyle = info.HitRate > 90 ? _goodStyle : 
                                       info.HitRate > 70 ? _normalStyle : _warningStyle;
                EditorGUILayout.LabelField($"命中率：{info.HitRate:F2}%", hitRateStyle);
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
        }
        
        private void DrawHealthCheck()
        {
            EditorGUILayout.LabelField("🏥 健康检查", _titleStyle);
            EditorGUILayout.Space(5);
            
            if (!Application.isPlaying)
            {
                return;
            }
            
            EditorGUILayout.BeginVertical("box");
            
            bool hasIssues = false;
            
            if (_stats.ETTaskPoolInfo.DuplicateReturns > 0)
            {
                EditorGUILayout.LabelField($"⚠️ 检测到 {_stats.ETTaskPoolInfo.DuplicateReturns} 次重复归还", 
                    _warningStyle);
                hasIssues = true;
            }
            
            if (_stats.ETTaskPoolInfo.HitRate < 70 && _stats.ETTaskPoolInfo.TotalAllocations > 100)
            {
                EditorGUILayout.LabelField($"⚠️ 命中率过低 ({_stats.ETTaskPoolInfo.HitRate:F1}%)，建议增加预热", 
                    _warningStyle);
                hasIssues = true;
            }
            
            float fillRate = _stats.ETTaskPoolInfo.MaxSize > 0 
                ? (float)_stats.ETTaskPoolInfo.PoolSize / _stats.ETTaskPoolInfo.MaxSize 
                : 0;
            
            if (fillRate > 0.95f)
            {
                EditorGUILayout.LabelField($"⚠️ 池接近满载 ({fillRate * 100:F1}%)，可能需要增加容量", 
                    _warningStyle);
                hasIssues = true;
            }
            
            if (!hasIssues)
            {
                EditorGUILayout.LabelField("✅ 对象池运行正常", _goodStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSeparator()
        {
            EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", 
                _separatorStyle);
        }
        
        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 刷新", GUILayout.Height(30)))
            {
                UpdateStats();
                Repaint();
            }
            
            if (GUILayout.Button("🧹 清空对象池", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    if (EditorUtility.DisplayDialog("确认", "确定要清空所有ETTask对象池吗？", "确定", "取消"))
                    {
                        ETTaskPoolStats.ClearAllPools();
                        UpdateStats();
                        Repaint();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在Play模式下执行此操作", "确定");
                }
            }
            
            if (GUILayout.Button("🔍 健康检查", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    ETTaskPoolStats.RunHealthCheck();
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在Play模式下执行此操作", "确定");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    /// <summary>
    /// ETTask对象池统计数据
    /// </summary>
    [Serializable]
    public class ETTaskPoolStats
    {
        public int PoolCount;
        public int TotalObjects;
        
        public ETTaskPoolInfo ETTaskPoolInfo = new ETTaskPoolInfo();
        public Dictionary<string, ETTaskPoolInfo> GenericPoolInfos = new Dictionary<string, ETTaskPoolInfo>();
        
        public void UpdateFromRuntime()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            // 使用ETTaskPoolView统一接口获取统计数据
            var stats = ETTaskPoolView.GetAllStats();
            PoolCount = stats.PoolCount;
            TotalObjects = stats.TotalObjects;
            ETTaskPoolInfo = stats.ETTaskPoolInfo;
            GenericPoolInfos = stats.GenericPoolInfos;
        }
        
        public static void ClearAllPools()
        {
            ETTaskPoolView.ClearAllPools();
        }
        
        public static void RunHealthCheck()
        {
            ETTaskPoolView.RunHealthCheck();
        }
    }
}
#endif
