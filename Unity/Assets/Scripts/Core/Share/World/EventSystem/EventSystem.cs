using System;
using System.Collections.Generic;
namespace ET
{
    public class EventSystem: Singleton<EventSystem>, ISingletonAwake
    {
        private class EventInfo
        {
            public IEvent IEvent { get; }
            
            public int SceneType {get; }

            public EventInfo(IEvent iEvent, int sceneType)
            {
                this.IEvent = iEvent;
                this.SceneType = sceneType;
            }
        }
        
        private readonly Dictionary<Type, Dictionary<long, IInvoke>> _allInvokeDic = new();
        
        private readonly Dictionary<Type, List<EventInfo>> _allEventDic = new();
        
        //private readonly Dictionary<Type, Dictionary<long, object>> allInvokers = new(); 
        
        public void Awake()
        {
            /*CodeTypes codeTypes = CodeTypes.Instance;
            foreach (Type type in codeTypes.GetTypes(typeof (EventAttribute)))
            {
                IEvent obj = Activator.CreateInstance(type) as IEvent;
                if (obj == null)
                {
                    throw new Exception($"type not is AEvent: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);
                foreach (object attr in attrs)
                {
                    EventAttribute eventAttribute = attr as EventAttribute;

                    Type eventType = obj.Type;

                    EventInfo eventInfo = new(obj, eventAttribute.SceneType);

                    if (!this._allEventDic.ContainsKey(eventType))
                    {
                        this._allEventDic.Add(eventType, new List<EventInfo>());
                    }
                    this._allEventDic[eventType].Add(eventInfo);
                }
            }*/

            /*foreach (Type type in codeTypes.GetTypes(typeof (InvokeAttribute)))
            {
                object obj = Activator.CreateInstance(type);
                IInvoke iInvoke = obj as IInvoke;
                if (iInvoke == null)
                {
                    throw new Exception($"type not is callback: {type.Name}");
                }
                
                object[] attrs = type.GetCustomAttributes(typeof(InvokeAttribute), false);
                foreach (object attr in attrs)
                {
                    if (!this.allInvokers.TryGetValue(iInvoke.Type, out var dict))
                    {
                        dict = new Dictionary<long, object>();
                        this.allInvokers.Add(iInvoke.Type, dict);
                    }
                    
                    InvokeAttribute invokeAttribute = attr as InvokeAttribute;
                    
                    try
                    {
                        dict.Add(invokeAttribute.Type, obj);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"action type duplicate: {iInvoke.Type.Name} {invokeAttribute.Type}", e);
                    }
                }
            }*/
        }

        public void RegisterEvent<T>(int sceneType) where T : IEvent, new()
        {
            T obj = new T();
            try
            {
                IEvent ieEvent = obj;
                EventInfo eventInfo = new EventInfo(obj, sceneType);
                if (!_allEventDic.TryGetValue(ieEvent.Type, out var list))
                {
                    list = new List<EventInfo>();
                    _allEventDic.Add(ieEvent.Type,list);
                }
                _allEventDic[ieEvent.Type].Add(eventInfo);
            }
            catch (Exception e)
            {
                throw new Exception($"type not is AEvent: {typeof(T).Name}", e);
            }
        }
        
        public void RegisterInvoke<T>(long attributeType = 0) where T : IInvoke, new()
        {
            T obj = new T();
            try
            {
                IInvoke iInvoke = obj;
                if (!_allInvokeDic.TryGetValue(iInvoke.Type, out var dict))
                {
                    dict = new Dictionary<long, IInvoke>();
                    _allInvokeDic.Add(iInvoke.Type, dict);
                }
                dict.Add(attributeType, obj);
                //Debug.LogError($"type : {typeof(T)}   {iInvoke.Type}  {attributeType}   {obj}");
            }
            catch (Exception e)
            {
                throw new Exception($"action type duplicate: {typeof(T).Name} {attributeType}", e);
            }
        }
        
        public async ETTask PublishAsync<S, T>(S scene, T a) where S: class, IScene where T : struct
        {
            List<EventInfo> iEvents;
            if (!this._allEventDic.TryGetValue(typeof(T), out iEvents))
            {
                Log.Warning($"PublishAsync error1: {typeof(T)} | {typeof(T).FullName} not found");
                return;
            }

            //TODO 
            //using ListComponent<ETTask> list = ListComponent<ETTask>.Create();
            List<ETTask> list = new List<ETTask>();
            
            foreach (EventInfo eventInfo in iEvents)
            {
                if (!SceneTypeSingleton.IsSame(scene.SceneType, eventInfo.SceneType))
                {
                    continue;
                }
                    
                if (!(eventInfo.IEvent is AEvent<S, T> aEvent))
                {
                    Log.Error($"event error: {eventInfo.IEvent.GetType().FullName}");
                    continue;
                }

                list.Add(aEvent.Handle(scene, a));
            }

            try
            {
                await ETTaskHelper.WaitAll(list);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void Publish<S, T>(S scene, T a) where S: class, IScene where T : struct
        {
            List<EventInfo> iEvents;
            if (!this._allEventDic.TryGetValue(typeof (T), out iEvents))
            {
                return;
            }

            int sceneType = scene.SceneType;
            foreach (EventInfo eventInfo in iEvents)
            {
                if (!SceneTypeSingleton.IsSame(sceneType, eventInfo.SceneType))
                {
                    continue;
                }

                
                if (!(eventInfo.IEvent is AEvent<S, T> aEvent))
                {
                    Log.Error($"event error: {eventInfo.IEvent.GetType().FullName}");
                    continue;
                }
                
                aEvent.Handle(scene, a).NoContext();
            }
        }
        
        // Invoke跟Publish的区别(特别注意)
        // Invoke类似函数，必须有被调用方，否则异常，调用者跟被调用者属于同一模块，比如MoveComponent中的Timer计时器，调用跟被调用的代码均属于移动模块
        // 既然Invoke跟函数一样，那么为什么不使用函数呢? 因为有时候不方便直接调用，比如Config加载，在客户端跟服务端加载方式不一样。比如TimerComponent需要根据Id分发
        // 注意，不要把Invoke当函数使用，这样会造成代码可读性降低，能用函数不要用Invoke
        // publish是事件，抛出去可以没人订阅，调用者跟被调用者属于两个模块，比如任务系统需要知道道具使用的信息，则订阅道具使用事件
        public void Invoke<A>(long type, A args) where A: struct
        {
            if (!this._allInvokeDic.TryGetValue(typeof(A), out var invokeHandlers))
            {
                throw new Exception($"Invoke error1: {type} {typeof(A).FullName} + " +
                                    $"如果同一个类型来自不同的程序集，即使它们的名称相同，它们在类型比较时也会被视为不同的类型。" +
                                    $"这是因为类型比较不仅考虑类型名称，还包括程序集信息");
            }
            if (!invokeHandlers.TryGetValue(type, out var invokeHandler))
            {
                throw new Exception($"Invoke error2: {type} {typeof(A).FullName}");
            }

            var aInvokeHandler = invokeHandler as AInvokeHandler<A>;
            if (aInvokeHandler == null)
            {
                throw new Exception($"Invoke error3, not AInvokeHandler: {type} {typeof(A).FullName}");
            }
            
            aInvokeHandler.Handle(args);
        }
        
        public T Invoke<A, T>(long type, A args) where A: struct
        {
            //<LogInvoker, ILog>(logInvoker);
            if (!this._allInvokeDic.TryGetValue(typeof(A), out var invokeHandlers))
            {
                throw new Exception($"Invoke error4: {type} {typeof(A).FullName} + " +
                                    $"如果同一个类型来自不同的程序集，即使它们的名称相同，它们在类型比较时也会被视为不同的类型。" +
                                    $"这是因为类型比较不仅考虑类型名称，还包括程序集信息");
            }
            
            if (!invokeHandlers.TryGetValue(type, out var invokeHandler))
            {
                throw new Exception($"Invoke error5: {type} {typeof(A).FullName}");
            }

            var aInvokeHandler = invokeHandler as AInvokeHandler<A, T>;
            if (aInvokeHandler == null)
            {
                throw new Exception($"Invoke error6, not AInvokeHandler: {type} {typeof(A).FullName} {typeof(T).FullName} ");
            }
            
            return aInvokeHandler.Handle(args);
        }
        
        public void Invoke<A>(A args) where A: struct
        {
            Invoke(0, args);
        }
        
        public T Invoke<A, T>(A args) where A: struct
        {
            return Invoke<A, T>(0, args);
        }
    }
}
