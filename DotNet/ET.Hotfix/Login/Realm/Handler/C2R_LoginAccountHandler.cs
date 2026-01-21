namespace ET;

[MessageSessionHandler(SceneType.Realm)]
public class C2R_LoginAccountHandler : MessageSessionHandler<C2R_LoginAccount,R2C_LoginAccount>
{
    protected override async ETTask Run(Session session, C2R_LoginAccount request, R2C_LoginAccount response)
    {
        session.RemoveComponent<SessionAcceptTimeoutComponent>();

        if (session.GetComponent<SessionLockingComponent>() != null)
        {
            response.Error = ErrorCode.ERR_RequestRepeatedly;
            session.Disconnect().NoContext();
            return;
        }
        
        if(string.IsNullOrEmpty(request.Account) || string.IsNullOrEmpty(request.Password))
        {
            response.Error = ErrorCode.ERR_LoginInfoIsNull;
            session.Disconnect().NoContext();
            return;
        }

        User user = null;
        List<Role> roleList = null;
        var coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount, request.Account.GetLongHashCode()))
            {
                var dbManager = session.Root().GetComponent<DBManagerComponent>();
                var db = dbManager.GetZoneDB(session.Zone());
                user = await db.QueryById<User>(request.Account);
                if (user != null)
                {
                    if (user.Status == UserStatus.Banned)
                    {
                        response.Error = ErrorCode.ERR_AccountInBlackList;
                        session.Disconnect().NoContext();
                        return;
                    }

                    if (user.Password != request.Password)
                    {
                        response.Error = ErrorCode.ERR_AccountNameOrPasswordError;
                        session.Disconnect().NoContext();
                        return;
                    }

                    roleList = await db.QueryByIds<Role, long>(user.RoleIds);
                }
                else
                {
                    long now = TimeInfo.Instance.ServerNow();
                    user = new User
                    {
                        Account = request.Account,
                        AccountType = AccountType.Phone,
                        UserId = GenerateIdManager.Instance.GenerateId(),
                        Username = $"User_{GenerateIdManager.Instance.GenerateId() % 1000000}",
                        Password = request.Password,
                        CreateTime = now,
                        Status = UserStatus.Normal,
                        RoleIds = new List<long>(),
                        Profile = new UserProfile
                        {
                            VipLevel = 0,
                            TotalRecharge = 0,
                            LastLoginTime = now
                        }
                    };
                    
                    long roleId = GenerateIdManager.Instance.GenerateId();
                    var defaultRole = RoleConfig.Instance.Get(GlobalConstConfig.Data.DefaultRoleId);
                    Role role = new Role
                    {
                        Id = roleId,
                        RoleConfigId = defaultRole.Id,
                        RoleName = defaultRole.Name,
                        Level = defaultRole.Level,
                        Job = defaultRole.Job,
                        CreateTime = now,
                    };
                    roleList = new List<Role>() { role };
                    user.RoleIds.Add(roleId);
                    user.LastRoleId = roleId;
                    await db.Save<Role, long>(role, roleId);
                    await db.Save<User, string>(user, request.Account);
                }
            }
        }

        var userId = user.UserId;
        var r2LAccountRequest =  R2L_AccountRequest.Create();
        r2LAccountRequest.UserId = userId;
        var messageSend = session.Root().GetComponent<MessageSender>();
        var l2RAccountResponse = (L2R_AccountResponse)await messageSend.Call(StartSceneConfigManager.Instance.LoginCenterActorId,r2LAccountRequest);
        if (l2RAccountResponse != null && l2RAccountResponse.Error != ErrorCode.ERR_Success)
        {
            response.Error = l2RAccountResponse.Error;
            session.Disconnect().NoContext();
            return;
        }

        var userSessionComponent = session.Root().GetComponent<UserSessionComponent>();
        var otherSession = userSessionComponent.Get(userId);
        if (otherSession != null && !otherSession.IsDisposed)
        {
            Log.Info($"清理Realm上的旧Session：用户 {userId}");
            otherSession.Disconnect().NoContext();
        }
        
        userSessionComponent.Add(userId, session);
        session.AddComponent<UserSessionTimeoutComponent, long>(userId);
        
        string token = TokenHelper.GenerateToken();
        var tokenComponent = session.Root().GetComponent<TokenComponent>();
        tokenComponent.Remove(userId);
        tokenComponent.Add(userId, token);
        
        response.Token = token;
        response.Error = ErrorCode.ERR_Success;

        SetUserInfo(response, user, roleList);
        await ETTask.CompletedTask;
    }

    private void SetUserInfo(R2C_LoginAccount response,User user, List<Role> roleList)
    {
        // 填充用户信息
        response.UserInfo = UserInfo.Create();
        response.UserInfo.Account = user.Account;
        response.UserInfo.UserId = user.UserId;
        response.UserInfo.Username = user.Username ?? $"User_{user.UserId % 1000000}";
        
        // 确保Profile不为null
        if (user.Profile != null)
        {
            response.UserInfo.VipLevel = user.Profile.VipLevel;
            response.UserInfo.TotalRecharge = user.Profile.TotalRecharge;
        }
        else
        {
            response.UserInfo.VipLevel = 0;
            response.UserInfo.TotalRecharge = 0;
        }
        
        if (roleList is { Count: > 0 })
        {
            foreach (var role in roleList)
            {
                RoleInfo roleInfo = new RoleInfo();
                roleInfo.RoleId = role.Id;
                roleInfo.RoleConfigId = role.RoleConfigId;
                response.UserInfo.RoleInfoList.Add(roleInfo);
                if (user.LastRoleId == role.Id)
                {
                    response.UserInfo.LastRoleInfo = roleInfo;
                }
            }

            if (response.UserInfo.LastRoleInfo == null)
            {
                response.UserInfo.LastRoleInfo = response.UserInfo.RoleInfoList[0];
            }
        }
    }
}