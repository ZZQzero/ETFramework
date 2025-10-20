using System;

namespace ET
{
    public static class MessageHelper
    {
        public static IResponse CreateResponse(Type requestType, int rpcId, int error)
        {
            //Type responseType = MessageOpcodeTypeMap.RequestResponse[requestType];
            IResponse response = MessageOpcodeTypeMap.RequestResponse[requestType](false);//(IResponse)ObjectPool.Fetch(responseType);
            response.Error = error;
            response.RpcId = rpcId;
            return response;
        }
    }
}