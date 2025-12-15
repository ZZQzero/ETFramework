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
            c2RGetRealmKey.Account = account;
            c2RGetRealmKey.Token = resp.Token;
            c2RGetRealmKey.ServerId = 3;
            var r2CGateRealmKey = (R2C_GetRealmKey) await clientSenderComponent.Call(c2RGetRealmKey);
            if (r2CGateRealmKey.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"get realm key failed {r2CGateRealmKey.Error}");
                return;
            }
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());

            // 如果已有角色，使用第一个角色ID；否则生成新角色ID
            long roleId;
            if (userComponent.HasRole())
            {
                roleId = userComponent.RoleIds[0];
                userComponent.CurrentRoleId = roleId;
                Log.Info($"使用已有角色：RoleId={roleId}");
            }
            else
            {
                roleId = GenerateIdManager.Instance.GenerateId();
                Log.Info($"创建新角色：RoleId={roleId}");
            }

            roleId = 123456;
            // 设置当前角色ID
            userComponent.CurrentRoleId = roleId;
            
            // 传递UserId和RoleId到服务端
            var netClient2MainLoginGame = await clientSenderComponent.LoginGameAsync(account, r2CGateRealmKey.Key, userComponent.UserId, roleId, r2CGateRealmKey.Address);
            
            if (netClient2MainLoginGame.Error == ErrorCode.ERR_Success)
            {
                Log.Info($"进入游戏成功，CurrentRoleId={userComponent.CurrentRoleId}");
            }
        }
    }
}