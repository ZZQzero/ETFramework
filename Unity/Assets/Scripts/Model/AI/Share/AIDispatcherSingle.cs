using System;
using System.Collections.Generic;

namespace ET
{
    public class AIDispatcherSingle: Singleton<AIDispatcherSingle>, ISingletonAwake
    {
        private readonly Dictionary<string, AAIHandler> _aiHandlers = new();
        
        public void Awake()
        {
        }
        
        public void RegisterAI<T>() where T : AAIHandler, new()
        {
            string key = typeof(T).Name;
            if (_aiHandlers.ContainsKey(key))
            {
                throw new InvalidOperationException($"AI handler already registered: {key}");
            }

            T handler = new T();
            _aiHandlers[key] = handler;
        }

        /// <summary>
        /// 获取 AI Handler
        /// </summary>
        public T Get<T>() where T : AAIHandler
        {
            string key = typeof(T).Name;
            return _aiHandlers.TryGetValue(key, out var handler) ? (T)handler : null;
        }
        
        public AAIHandler Get(string key)
        {
            this._aiHandlers.TryGetValue(key, out var aaiHandler);
            return aaiHandler;
        }
    }
}