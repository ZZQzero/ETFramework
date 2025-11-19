namespace ET
{
    public interface IConsoleHandler
    {
        ETTask Run(Fiber fiber, ModeContext context, string content);
    }
}