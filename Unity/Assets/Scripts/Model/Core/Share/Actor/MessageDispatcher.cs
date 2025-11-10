using System;
using System.Collections.Generic;

namespace ET
{
    public struct MessageDispatcherInfo
    {
        public int SceneType { get; }
        
        public IMHandler IMHandler { get; }

        public MessageDispatcherInfo(int sceneType, IMHandler imHandler)
        {
            this.SceneType = sceneType;
            this.IMHandler = imHandler;
        }
    }
    
    /// <summary>
    /// Actor消息分发组件
    /// </summary>
    public class MessageDispatcher: Singleton<MessageDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, List<MessageDispatcherInfo>> messageHandlers = new();

        public void Awake()
        {
        }
        
        public void RegisterMessage<T>(int sceneType) where T : IMHandler, new()
        {
            IMHandler imHandler = new T();
            Type messageType = imHandler.GetRequestType();

            Type handleResponseType = imHandler.GetResponseType();
            if (handleResponseType != null)
            {
                Type responseType = MessageOpcodeTypeMap.RequestResponseType[messageType];
                if (handleResponseType != responseType)
                {
                    throw new Exception($"message handler response type error: {messageType.FullName}");
                }
            }
            
            MessageDispatcherInfo messageDispatcherInfo = new(sceneType, imHandler);

            RegisterHandler(messageType, messageDispatcherInfo);
        }
        
        private void RegisterHandler(Type type, MessageDispatcherInfo handler)
        {
            if (!this.messageHandlers.ContainsKey(type))
            {
                this.messageHandlers.Add(type, new List<MessageDispatcherInfo>());
            }

            this.messageHandlers[type].Add(handler);
        }

        public async ETTask Handle(Entity entity, Address fromAddress, MessageObject message)
        {
            List<MessageDispatcherInfo> list;
            if (!this.messageHandlers.TryGetValue(message.GetType(), out list))
            {
                Log.Error($"not found message handler: {message} {entity.GetType().FullName}");
                return;
            }

            int sceneType = entity.IScene.SceneType;
            foreach (MessageDispatcherInfo actorMessageDispatcherInfo in list)
            {
                if (!SceneTypeSingleton.IsSame(actorMessageDispatcherInfo.SceneType, sceneType))
                {
                    continue;
                }
                
                try
                {
                    await actorMessageDispatcherInfo.IMHandler.Handle(entity, fromAddress, message);
                }
                catch (Exception e)
                {
                    Log.Error($"消息处理异常: {message.GetType().FullName}\n{e}");
                }
            }
        }
    }
}