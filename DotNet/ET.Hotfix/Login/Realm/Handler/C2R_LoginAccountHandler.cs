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
                    await db.Save<User, string>(user, request.Account);
                }
                Log.Info($" C2R_LoginAccountHandler: {session.Root().Name} | {dbManager}  {db}");
            }
        }

        var userId = user.UserId;
        var r2LAccountRequest =  R2L_AccountRequest.Create();
        r2LAccountRequest.UserId = userId;
        var config = StartSceneConfig.Instance.GetOrDefault(202);
        var l2RAccountResponse = await session.Root().GetComponent<MessageSender>().Call(config.ActorId,r2LAccountRequest) as L2R_AccountResponse;
        if (l2RAccountResponse.Error != ErrorCode.ERR_Success)
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
        
        // 确保RoleIds不为null
        if (user.RoleIds != null && user.RoleIds.Count > 0)
        {
            response.UserInfo.RoleIds = new List<long>(user.RoleIds);
        }
        else
        {
            response.UserInfo.RoleIds = new List<long>();
        }
        await ETTask.CompletedTask;
    }
}