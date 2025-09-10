using System;
using System.Collections.Generic;

namespace ET
{
    public struct NumericWatcherInfo
    {
        public int SceneType { get; }
        public INumericWatcher INumericWatcher { get; }

        public NumericWatcherInfo(int sceneType, INumericWatcher numericWatcher)
        {
            this.SceneType = sceneType;
            this.INumericWatcher = numericWatcher;
        }
    }

    /// <summary>
    /// 监视数值变化组件,分发监听
    /// </summary>
    public class NumericWatcherComponent : Singleton<NumericWatcherComponent>, ISingletonAwake
    {
        private readonly Dictionary<int, List<NumericWatcherInfo>> _allWatchers = new();

        public void Awake()
        {
        }

        /// <summary>
        /// 注册数值
        /// </summary>
        public void RegisterNumeric<T>(int numericType, int sceneType) where T : INumericWatcher, new()
        {
            var obj = new T();
            var watcherInfo = new NumericWatcherInfo(sceneType, obj);

            if (!_allWatchers.TryGetValue(numericType, out var list))
            {
                list = new List<NumericWatcherInfo>();
                _allWatchers[numericType] = list;
            }

            list.Add(watcherInfo);
        }

        /// <summary>
        /// 分发事件
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            if (!this._allWatchers.TryGetValue(args.NumericType, out var list))
            {
                return;
            }

            int unitDomainSceneType = unit.IScene.SceneType;
            foreach (NumericWatcherInfo watcher in list)
            {
                if (!SceneTypeSingleton.IsSame(watcher.SceneType, unitDomainSceneType))
                {
                    continue;
                }
                watcher.INumericWatcher.Run(unit, args);
            }
        }
    }
}