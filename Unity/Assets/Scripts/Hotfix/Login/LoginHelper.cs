namespace ET
{
    public static class LoginHelper
    {
        public static async ETTask Login(Scene root, string account, string password)
        {
            ClientSenderComponent clientSenderComponent = root.GetComponent<ClientSenderComponent>();
            var resp = await clientSenderComponent.LoginAsync(GlobalConfigManager.Instance.Config.IPAddress, account, password);
            if (resp.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"login failed {resp.Error}");
                return;
            }
            
            Log.Info($"登录成功 {root.Zone()}");
            
            // 存储用户信息到UserComponent（全局访问）
            UserComponent userComponent = root.GetComponent<UserComponent>();
            if (userComponent == null)
            {
                userComponent = root.AddComponent<UserComponent>();
            }
            userComponent.SetUserInfo(resp.UserInfo);
            
            C2R_GetRealmKey c2RGetRealmKey = C2R_GetRealmKey.Create();
            c2RGetRealmKey.UserId = resp.UserInfo.UserId;
            c2RGetRealmKey.Token = resp.Token;
            c2RGetRealmKey.ServerId = 3;
            var r2CGateRealmKey = (R2C_GetRealmKey) await clientSenderComponent.Call(c2RGetRealmKey);
            if (r2CGateRealmKey.Error != ErrorCode.ERR_Success)
            {
                clientSenderComponent.NectClientDisconnect();
                Log.Error($"get realm key failed {r2CGateRealmKey.Error}");
                return;
            }
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
            
            // 传递UserId和RoleId到服务端
            var netClient2MainLoginGame = await clientSenderComponent.LoginGameAsync(account, r2CGateRealmKey.Key, userComponent.UserId, userComponent.CurrentRole, r2CGateRealmKey.Address);
            if (netClient2MainLoginGame.Error != ErrorCode.ERR_Success)
            {
                clientSenderComponent.NectClientDisconnect();
                return;
            }
            Log.Info($"进入游戏成功，CurrentRoleId={userComponent.CurrentRole}");
        }
    }
}