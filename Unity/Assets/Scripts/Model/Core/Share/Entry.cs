using System;

namespace ET
{
    public struct EntryEvent1
    {
    }   
    
    public struct EntryEvent2
    {
    } 
    
    public struct EntryEvent3
    {
    }
    
    public static class Entry
    {
        
        public static void Start()
        {
            StartAsync().NoContext();
        }
        
        private static async ETTask StartAsync()
        {
            WinPeriod.Init();

            // 注册Mongo type
#if DOTNET
            MongoRegister.Init();
#endif
            
            MemoryPackRegister.Init();
            
            // 注册Entity序列化器
            //EntitySerializeRegister.Init();
            
            //await World.Instance.AddSingleton<ConfigLoader>().LoadAsync();
            Log.Info("Entry Start");
            var test = AIConfigConfigCategory.Instance.GetDataList();
            foreach (var item in test)
            {
                Log.Error($"{item.Name}   {item.Id}   {item.AIConfigId}  {item.Desc}");
            }
            await FiberManager.Instance.Create(SchedulerType.Main, SceneType.Main, 0, SceneType.Main, "Main");
        }
    }
}