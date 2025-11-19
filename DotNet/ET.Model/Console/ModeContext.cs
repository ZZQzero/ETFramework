namespace ET
{
    [ComponentOf(typeof(ConsoleComponent))]
    public class ModeContext: Entity, IAwake, IDestroy
    {
        public string Mode = "";
    }
}