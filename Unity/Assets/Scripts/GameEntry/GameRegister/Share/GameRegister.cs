using System;


namespace ET
{
    public static partial class GameRegister
    {
        public static void RegisterSingleton()
        {
            World.Instance.AddSingleton<TableConfigManager>();
            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            World.Instance.AddSingleton<ObjectPool>();
            World.Instance.AddSingleton<GenerateIdManager>();
            World.Instance.AddSingleton<MessageQueue>();
            World.Instance.AddSingleton<NetServices>();
#if DOTNET
            World.Instance.AddSingleton<StartSceneConfigManager>();  
#endif
            ReloadSingleton();
        }

        private static void ReloadSingleton()
        {
            World.Instance.AddSingleton<EventSystem>();
            World.Instance.AddSingleton<EntitySystemSingleton>();
            World.Instance.AddSingleton<MessageSessionDispatcher>();
            World.Instance.AddSingleton<MessageDispatcher>();
            World.Instance.AddSingleton<AIDispatcherSingle>();
            World.Instance.AddSingleton<NumericWatcherComponent>();
#if UNITY
#endif

#if DOTNET
            World.Instance.AddSingleton<ConsoleDispatcher>();
            World.Instance.AddSingleton<HttpDispatcher>();
#endif
        }
    }
}