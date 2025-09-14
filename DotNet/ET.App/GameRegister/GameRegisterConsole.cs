

namespace ET;

public static partial class GameRegister
{
    public static void RegisterConsole()
    {
        ConsoleDispatcher.Instance.RegisterConsole<ReloadConfigConsoleHandler>(ConsoleMode.ReloadConfig);
        ConsoleDispatcher.Instance.RegisterConsole<ReloadDllConsoleHandler>(ConsoleMode.ReloadDll);
        ConsoleDispatcher.Instance.RegisterConsole<CreateRobotConsoleHandler>(ConsoleMode.CreateRobot);
    }
}