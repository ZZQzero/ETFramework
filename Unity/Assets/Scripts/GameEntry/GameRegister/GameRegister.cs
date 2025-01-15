using System;
using ET.Client;
#if DOTNET
using ET.Server;
#endif

namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterSingleton()
        {
            World.Instance.AddSingleton<CodeTypes>();
            World.Instance.AddSingleton<TableConfigManager>();
            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<IdGenerater>();
            World.Instance.AddSingleton<OpcodeType>();
            World.Instance.AddSingleton<MessageQueue>();
            World.Instance.AddSingleton<NetServices>();
            
            LogMsg logMsg = World.Instance.AddSingleton<LogMsg>();
            logMsg.AddIgnore(LoginOuter.C2G_Ping);
            logMsg.AddIgnore(LoginOuter.G2C_Ping);
            
            ReloadSingleton();
        }

        private static void ReloadSingleton()
        {
            World.Instance.AddSingleton<EventSystem>();
            World.Instance.AddSingleton<EntitySystemSingleton>();
            World.Instance.AddSingleton<MessageSessionDispatcher>();
            World.Instance.AddSingleton<MessageDispatcher>();
            World.Instance.AddSingleton<AIDispatcherComponent>();
            World.Instance.AddSingleton<NumericWatcherComponent>();
#if UNITY
            World.Instance.AddSingleton<UIEventComponent>();
#endif

#if DOTNET
            World.Instance.AddSingleton<ConsoleDispatcher>();
            World.Instance.AddSingleton<HttpDispatcher>();
#endif
        }
    }
}