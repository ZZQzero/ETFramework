namespace ET
{
    [MessageHandler(SceneType.NetClient)]
    public class Main2NetClient_LoginGameHandler : MessageHandler<Scene,Main2NetClient_LoginGame,NetClient2Main_LoginGame>
    {
        protected override async ETTask Run(Scene root, Main2NetClient_LoginGame request, NetClient2Main_LoginGame response)
        {
            string account = request.Account;
            //创建一个gateSession,并且保存到SessionComponent中
            NetComponent netComponent = root.GetComponent<NetComponent>();
            Session gateSession = await netComponent.CreateRouterSession(NetworkHelper.ToIPEndPoint(request.GateAddress), account,account);
            gateSession.AddComponent<ClientSessionErrorComponent>();
            root.GetComponent<SessionComponent>().Session = gateSession;
            C2G_LoginGameGate c2GLoginGameGate = C2G_LoginGameGate.Create();
            c2GLoginGameGate.Key = request.RealmKey;
            c2GLoginGameGate.AccountName = request.Account;
            c2GLoginGameGate.UserId = request.UserId;
            c2GLoginGameGate.RoleId = request.RoleId;
            G2C_LoginGameGate g2CLoginGameGate = (G2C_LoginGameGate) await gateSession.Call(c2GLoginGameGate);

            if (g2CLoginGameGate.Error != ErrorCode.ERR_Success)
            {
                response.Error = g2CLoginGameGate.Error;
                Log.Error($"登录Gate失败 {g2CLoginGameGate.Error}");
                return;
            }
            Log.Debug("登录Gate成功！");
            var g2CEnterGame = (G2C_EnterGame) await gateSession.Call(C2G_EnterGame.Create());
            if (g2CEnterGame.Error != ErrorCode.ERR_Success)
            {
                response.Error = g2CEnterGame.Error;
                Log.Error($"登录Map失败 {g2CEnterGame.Error}");
                return;
            }
            response.Error = ErrorCode.ERR_Success;
        }
    }
}