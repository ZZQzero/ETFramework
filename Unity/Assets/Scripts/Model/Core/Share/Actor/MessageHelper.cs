using System;

namespace ET
{
    public static class MessageHelper
    {
        public static IResponse CreateResponse(Type requestType, int rpcId, int error)
        {
            IResponse response = MessageOpcodeTypeMap.RequestResponse[requestType](false);
            response.Error = error;
            response.RpcId = rpcId;
            return response;
        }
    }
}