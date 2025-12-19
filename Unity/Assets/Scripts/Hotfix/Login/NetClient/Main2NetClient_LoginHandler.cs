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
            
#if UNITY_WEBGL
            var netComponent = root.AddComponent<NetComponent, IKcpTransport>(new WebSocketTransport(routerAddressComponent.AddressFamily));
#else
            var netComponent = root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(routerAddressComponent.AddressFamily));
#endif
            root.GetComponent<FiberParentComponent>().ParentFiberId = request.OwnerFiberId;
            
            IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(request.Account);
            Session session = await netComponent.CreateRouterSession(realmAddress, request.Account, request.Password);
            
            C2R_LoginAccount c2RLogin = C2R_LoginAccount.Create();
            c2RLogin.Account = request.Account;
            c2RLogin.Password = request.Password;
            var r2CLoginAccount = (R2C_LoginAccount)await session.Call(c2RLogin);
            
            if(r2CLoginAccount.Error != ErrorCode.ERR_Success)
            {
                session.Dispose();
                root.RemoveComponent<NetComponent>();
                response.Error = r2CLoginAccount.Error;
                return;
            }
    
            root.AddComponent<SessionComponent>().Session = session;
            response.Token = r2CLoginAccount.Token;
            response.UserInfo = r2CLoginAccount.UserInfo;
            response.Error = r2CLoginAccount.Error;
        }
    }
}