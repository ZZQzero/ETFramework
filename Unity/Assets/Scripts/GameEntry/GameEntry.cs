using System.Diagnostics;
using UnityEngine;

namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        private void Awake()
        {
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEvent();
            GameRegister.RegisterInvoke();
            GameRegister.RegisterMessage();
            GameRegister.RegisterMessageSession();
            GameRegister.RegisterAI();
            GameRegister.RegisterEntitySystem();
            StartAsync().NoContext();
        }
        
        private async ETTask StartAsync()
        {
            WinPeriod.Init();
            Log.Info("Entry Start");
            await FiberManager.Instance.Create(SchedulerType.Main, SceneType.Main, 0, SceneType.Main, "Main");
        }
    }
}
