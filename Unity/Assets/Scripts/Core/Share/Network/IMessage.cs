using Nino.Core;

namespace ET
{
    // 不需要返回消息
    [NinoType]
    public interface IMessage
    {
    }

    [NinoType]
    public interface IRequest: IMessage
    {
        int RpcId
        {
            get;
            set;
        }
    }

    [NinoType]
    public interface IResponse: IMessage
    {
        int Error
        {
            get;
            set;
        }

        string Message
        {
            get;
            set;
        }

        int RpcId
        {
            get;
            set;
        }
    }
}