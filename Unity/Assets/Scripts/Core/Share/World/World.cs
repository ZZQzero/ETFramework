using System;
using System.Collections.Generic;

namespace ET
{
    public class World: IDisposable
    {
        [StaticField]
        private static World instance;

        [StaticField]
        public static World Instance
        {
            get
            {
                return instance ??= new World();
            }
        }

        private readonly object _syncRoot = new();
        private readonly Stack<Type> stack = new();
        private readonly Dictionary<Type, ASingleton> singletons = new();
        
        private World()
        {
        }
        
        public void Dispose()
        {
            instance = null;
            
            lock (_syncRoot)
            {
                while (this.stack.Count > 0)
                {
                    Type type = this.stack.Pop();
                    if (this.singletons.Remove(type, out ASingleton singleton))
                    {
                        singleton.Dispose();
                    }
                }

                // dispose剩下的singleton，主要为了把instance置空
                foreach (var kv in this.singletons)
                {
                    kv.Value.Dispose();
                }
                singletons.Clear();
            }
        }

        public T AddSingleton<T>() where T : ASingleton, ISingletonAwake, new()
        {
            if (this.singletons.ContainsKey(typeof(T)))
            {
                Log.Error($"有重复添加:{typeof(T)}");
                return (T)this.singletons[typeof(T)];
            }
            T singleton = new();
            singleton.Awake();

            AddSingleton(singleton,typeof(T));
            return singleton;
        }
        
        public T AddSingleton<T, A>(A a) where T : ASingleton, ISingletonAwake<A>, new()
        {
            if (this.singletons.ContainsKey(typeof(T)))
            {
                Log.Error($"有重复添加:{typeof(T)}");
                return (T)this.singletons[typeof(T)];
            }
            T singleton = new();
            singleton.Awake(a);

            AddSingleton(singleton,typeof(T));
            return singleton;
        }
        
        public T AddSingleton<T, A, B>(A a, B b) where T : ASingleton, ISingletonAwake<A, B>, new()
        {
            if (this.singletons.ContainsKey(typeof(T)))
            {
                Log.Error($"有重复添加:{typeof(T)}");
                return (T)this.singletons[typeof(T)];
            }
            T singleton = new();
            singleton.Awake(a, b);

            AddSingleton(singleton,typeof(T));
            return singleton;
        }
        
        public T AddSingleton<T, A, B, C>(A a, B b, C c) where T : ASingleton, ISingletonAwake<A, B, C>, new()
        {
            if (this.singletons.ContainsKey(typeof(T)))
            {
                Log.Error($"有重复添加:{typeof(T)}");
                return (T)this.singletons[typeof(T)];
            }
            T singleton = new();
            singleton.Awake(a, b, c);

            AddSingleton(singleton,typeof(T));
            return singleton;
        }

        public void AddSingleton(ASingleton singleton,Type type)
        {
            lock (_syncRoot)
            {
                if (singleton is ISingletonReverseDispose)
                {
                    this.stack.Push(type);
                }
                singletons.Add(type, singleton);
            }

            singleton.Register();
        }
    }
}