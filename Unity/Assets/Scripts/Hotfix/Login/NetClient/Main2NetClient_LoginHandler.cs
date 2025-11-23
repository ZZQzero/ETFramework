using System;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_LoginHandler: MessageHandler<Scene, Main2NetClient_Login, NetClient2Main_Login>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Login request, NetClient2Main_Login response)
        {
            Log.Error($"Main2NetClient_LoginHandler {root.Name}");
            RouterAddressComponent routerAddressComponent = root.GetComponent<RouterAddressComponent>();
            await routerAddressComponent.GetAllRouter();
            
            NetComponent netComponent = root.GetComponent<NetComponent>();
            root.GetComponent<FiberParentComponent>().ParentFiberId = request.OwnerFiberId;
            
            IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(request.Account);
            Session session = await netComponent.CreateRouterSession(realmAddress, request.Account, request.Password);
            
            C2R_LoginAccount c2RLogin = C2R_LoginAccount.Create();
            c2RLogin.Account = request.Account;
            c2RLogin.Password = request.Password;
            var r2CLoginAccount = (R2C_LoginAccount)await session.Call(c2RLogin);
            
            if(r2CLoginAccount.Error == ErrorCode.ERR_Success)
            {
                root.AddComponent<SessionComponent>().Session = session;
            }
            else
            {
                session.Dispose();
            }
            response.Token = r2CLoginAccount.Token;
            response.Error = r2CLoginAccount.Error;
        }
    }
}