#if DOTNET

using CommandLine;

namespace ET
{
    public class GameServer
    {
        public static void Init()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Error(e.ExceptionObject.ToString());
                };
				
                // 命令行参数
                Parser.Default.ParseArguments<Options>(System.Environment.GetCommandLineArgs())
                        .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                        .WithParsed((o)=>World.Instance.AddSingleton(o,typeof(Options)));
                Options.Instance.SceneName = "StateSync";
                Options.Instance.Develop = 1;
                Options.Instance.Console = 1;
                World.Instance.AddSingleton<Logger>().Log = new NLogger(Options.Instance.SceneName, Options.Instance.Process, 0);
				
                ETTask.ExceptionHandler += Log.Error;
                World.Instance.AddSingleton<TimeInfo>();
                World.Instance.AddSingleton<FiberManager>();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        public static async ETTask StartAsync()
        {
            WinPeriod.Init();
            MongoRegister.Init();
            await FiberManager.Instance.Create(SchedulerType.Main, SceneType.Main, 0, SceneType.Main, "Main");
        }

        public static void Register()
        {
            GameRegister.RegisterSingleton();
            GameRegister.RegisterEntitySystem();
            
            GameRegisterHotfix.RegisterEventAuto();
            GameRegisterHotfix.RegisterInvokeAuto();
            GameRegisterHotfix.RegisterMessageAuto();
            GameRegisterHotfix.RegisterMessageSessionAuto();
            GameRegisterHotfix.RegisterHttpAuto();
            GameRegisterHotfix.RegisterConsoleAuto();
        }

        public static void Update()
        {
            TimeInfo.Instance.Update();
            FiberManager.Instance.Update();
        }

        public static void LateUpdate()
        {
            FiberManager.Instance.LateUpdate();
        }
    }
}
#endif