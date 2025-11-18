namespace ET
{
    
    public struct MailBoxInvoker
    {
        public Address FromAddress;
        public MessageObject MessageObject;
        public MailBoxComponent MailBoxComponent;
    }
    
    /// <summary>
    /// 挂上这个组件表示该Entity是一个Actor,接收的消息将会队列处理
    /// </summary>
    [ComponentOf]
    public class MailBoxComponent: Entity, IAwake<int>, IDestroy
    {
        public long ParentInstanceId { get; set; }
        // Mailbox的类型
        public int MailBoxType { get; set; }
    }
}