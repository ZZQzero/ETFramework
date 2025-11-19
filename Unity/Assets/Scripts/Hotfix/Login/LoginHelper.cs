namespace ET
{
    public static class LoginHelper
    {
        public static async ETTask Login(Scene root, string account, string password)
        {
            /*root.RemoveComponent<ClientSenderComponent>();
            ClientSenderComponent clientSenderComponent = root.AddComponent<ClientSenderComponent>();*/
            ClientSenderComponent clientSenderComponent = root.GetComponent<ClientSenderComponent>();
            var resp = await clientSenderComponent.LoginAsync(GlobalConfigManager.Instance.Config.IPAddress, account, password);
            if (resp.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"login failed {resp.Error}");
                return;
            }
            
            Log.Error($"登录成功 {root.Zone()}");
            C2R_GetRealmKey c2RGetRealmKey = C2R_GetRealmKey.Create();
            c2RGetRealmKey.Account = account;
            c2RGetRealmKey.Token = resp.Token;
            // 这里一般会有创角，选择区服，demo就不做这个操作了，直接放在3区
            c2RGetRealmKey.ServerId = 3;
            var r2CGateRealmKey = (R2C_GetRealmKey) await clientSenderComponent.Call(c2RGetRealmKey);
            if (r2CGateRealmKey.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"get realm key failed {r2CGateRealmKey.Error}");
                return;
            }

            var roleId = 1;
            var netClient2MainLoginGame = await clientSenderComponent.LoginGameAsync(account, r2CGateRealmKey.Key, roleId, r2CGateRealmKey.Address);
            
            //root.GetComponent<PlayerComponent>().MyId = resp.PlayerId;
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
        }
    }
}