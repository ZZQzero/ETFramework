namespace ET
{
    public class UnityLogInvoke: AInvokeHandler<LogInvoker, ILog>
    {
        public override ILog Handle(LogInvoker args)
        {
            return new UnityLogger();
        }
    }
}