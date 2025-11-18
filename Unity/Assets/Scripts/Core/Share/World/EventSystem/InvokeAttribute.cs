namespace ET
{
    public class InvokeAttribute: BaseAttribute
    {
        public long Type { get; }

        public InvokeAttribute(long type)
        {
            this.Type = type;
        }
    }
}