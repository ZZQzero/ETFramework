using System;
using System.Threading;
using CommandLine;

namespace ET
{
    public static class ServerLauncher
    {
        public static void Main()
        {
            GameServer.Init();
            GameServer.Register();
            GameServer.StartAsync().NoContext();
            
            while (true)
            {
                Thread.Sleep(1);
                try
                {
                    GameServer.Update();
                    GameServer.LateUpdate();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}