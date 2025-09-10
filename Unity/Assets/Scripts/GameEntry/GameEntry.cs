using System.Diagnostics;
using UnityEngine;

namespace ET
{
    public class GameEntry : MonoBehaviour
    {
        void Awake()
        {
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEvent();
            GameRegister.RegisterInvoke();
            GameRegister.RegisterMessage();
            GameRegister.RegisterMessageSession();
            GameRegister.RegisterAI();
            GameRegister.RegisterEntitySystem();
            Entry.Start();
            /*int n = 1000000;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < n; i++)
            {
               var obj = ObjectFastPool.Fetch<G2R_GetLoginKey>();
               ObjectFastPool.Recycle(obj);
            }
            sw.Stop();
            Debug.LogError($"耗时 {sw.ElapsedMilliseconds}ms");*/
            
            
            /*sw.Restart();
            for (int i = 0; i < n; i++)
            {
                var obj = ObjectPool.Fetch<G2R_GetLoginKey>();
                ObjectPool.Recycle(obj);
            }
            sw.Stop();
            Debug.LogError($"耗时 {sw.ElapsedMilliseconds}ms");*/
        }
    }
}
