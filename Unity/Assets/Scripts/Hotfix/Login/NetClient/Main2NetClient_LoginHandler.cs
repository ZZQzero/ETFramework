using System;
using System.Net;
using System.Net.Sockets;

namespace ET
{
    public class Main2NetClient_LoginHandler: MessageHandler<Scene, Main2NetClient_Login, NetClient2Main_Login>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_Login request, NetClient2Main_Login response)
        {
            string account = request.Account;
            string password = request.Password;
            // 创建一个ETModel层的Session
            root.RemoveComponent<RouterAddressComponent>();
            // 获取路由跟realmDispatcher地址
            RouterAddressComponent routerAddressComponent = root.AddComponent<RouterAddressComponent, string>(request.Address);
            await routerAddressComponent.GetAllRouter();
#if UNITY_WEBGL
            root.AddComponent<NetComponent, IKcpTransport>(new WebSocketTransport(routerAddressComponent.AddressFamily));
#else
            root.AddComponent<NetComponent, IKcpTransport>(new UdpTransport(routerAddressComponent.AddressFamily));
#endif
            root.GetComponent<FiberParentComponent>().ParentFiberId = request.OwnerFiberId;

            NetComponent netComponent = root.GetComponent<NetComponent>();
            
            IPEndPoint realmAddress = routerAddressComponent.GetRealmAddress(account);

            Session session = await netComponent.CreateRouterSession(realmAddress, account, password);
            C2R_LoginAccount c2RLogin = C2R_LoginAccount.Create();
            c2RLogin.Account = account;
            c2RLogin.Password = password;
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