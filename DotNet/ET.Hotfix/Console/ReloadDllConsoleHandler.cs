namespace ET
{
    [ConsoleHandler(ConsoleMode.ReloadDll)]
    public class ReloadDllConsoleHandler: IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContext context, string content)
        {
            await ETTask.CompletedTask;
            //CodeLoader.Instance.Reload();
        }
    }
}