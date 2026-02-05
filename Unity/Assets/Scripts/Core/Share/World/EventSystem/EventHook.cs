using System;

namespace ET
{
    /// <summary>
    /// 轻量事件钩子：允许在数据组件中安全聚合事件，不暴露覆盖赋值。
    /// </summary>
    public struct EventHook
    {
        private Action handlers;

        public void Add(Action handler) => handlers += handler;
        public void Remove(Action handler) => handlers -= handler;
        public void Invoke() => handlers?.Invoke();
        public void Clear() => handlers = null;
        public bool HasListeners => handlers != null;
    }

    /// <summary>
    /// 轻量事件钩子（带一个参数）。
    /// </summary>
    public struct EventHook<T>
    {
        private Action<T> handlers;

        public void Add(Action<T> handler) => handlers += handler;
        public void Remove(Action<T> handler) => handlers -= handler;
        public void Invoke(T value) => handlers?.Invoke(value);
        public void Clear() => handlers = null;
        public bool HasListeners => handlers != null;
    }
}