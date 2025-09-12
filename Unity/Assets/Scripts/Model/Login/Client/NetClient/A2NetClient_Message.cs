namespace ET
{
    [Message]
    public class A2NetClient_Message: MessageObject, IMessage
    {
        public static A2NetClient_Message Create()
        {
            return ObjectPool.Fetch<A2NetClient_Message>();
        }

        public override void Dispose()
        {
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }
        
        public IMessage MessageObject;
    }
    
    [Message]
    [ResponseType(nameof(A2NetClient_Response))]
    public class A2NetClient_Request: MessageObject, IRequest
    {
        public static A2NetClient_Request Create()
        {
            return ObjectPool.Fetch<A2NetClient_Request>();
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }
     
        public int RpcId { get; set; }
        public IRequest MessageObject;
    }
    
    [Message]
    public class A2NetClient_Response: MessageObject, IResponse
    {
        public static A2NetClient_Response Create()
        {
            return ObjectPool.Fetch<A2NetClient_Response>();
        }

        public override void Dispose()
        {
            this.RpcId = default;
            this.Error = default;
            this.Message = default;
            this.MessageObject = default;
            ObjectPool.Recycle(this);
        }

        public int RpcId { get; set; }
        public int Error { get; set; }
        public string Message { get; set; }
        
        public IResponse MessageObject;
    }
    
    [Message]
    public class NetClient2Main_SessionDispose: MessageObject, IMessage
    {
        public static NetClient2Main_SessionDispose Create()
        {
            return ObjectPool.Fetch<NetClient2Main_SessionDispose>();
        }
        

        public override void Dispose()
        {
            ObjectPool.Recycle(this);
        }
        
        public int Error { get; set; }
    }
}