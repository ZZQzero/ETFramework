namespace ET;

[MessageSessionHandler(SceneType.Realm)]
public class C2R_LoginAccountHandler : MessageSessionHandler<C2R_LoginAccount,R2C_LoginAccount>
{
    protected override async ETTask Run(Session session, C2R_LoginAccount request, R2C_LoginAccount response)
    {
        Log.Info(" C2R_LoginAccountHandler");
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

        var coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount, request.Account.GetLongHashCode()))
            {
                var dbManager = session.Root().GetComponent<DBManagerComponent>();
                var db = dbManager.GetZoneDB(session.Zone());
                var account = await db.QuerySingle<Account>(a => a.AccountName.Equals(request.Account));
                if (account != null)
                {
                    //session.AddChild(account);
                    if (account.AccountType == AccountType.BlackList)
                    {
                        response.Error = ErrorCode.ERR_AccountInBlackList;
                        session.Disconnect().NoContext();
                        return;
                    }

                    if (account.Password != request.Password)
                    {
                        response.Error = ErrorCode.ERR_AccountNameOrPasswordError;
                        session.Disconnect().NoContext();
                        return;
                    }
                }
                else
                {
                    account = new Account();
                    account.AccountType = AccountType.General;
                    account.AccountName = request.Account;
                    account.Password = request.Password;
                    account.CreateTime = TimeInfo.Instance.ServerNow();
                    await db.Save<Account>(account);
                }
                Log.Info($" C2R_LoginAccountHandler: {session.Root().Name} | {dbManager}  {db}");
            }
        }

        var r2LAccountRequest =  R2L_AccountRequest.Create();
        r2LAccountRequest.Account = request.Account;
        var config = StartSceneConfig.Instance.GetOrDefault(202);
        Log.Info($"{session.Fiber().Id}  {session.Root().Name}   ");
        var l2RAccountResponse = await session.Root().GetComponent<MessageSender>().Call(config.ActorId,r2LAccountRequest) as L2R_AccountResponse;
        if (l2RAccountResponse.Error != ErrorCode.ERR_Success)
        {
            response.Error = l2RAccountResponse.Error;
            session.Disconnect().NoContext();
            return;
        }

        var accountSessionComponent = session.Root().GetComponent<AccountSessionComponent>();
        var otherSession = accountSessionComponent.Get(request.Account);
        if (otherSession != null)
        {
            var a2CDisconnect = A2C_Disconnect.Create();
            a2CDisconnect.Error = ErrorCode.ERR_AccountDifferentLocation;
            otherSession.Send(a2CDisconnect);
            otherSession.Disconnect().NoContext();
        }
        
        accountSessionComponent.AddAccountSession(request.Account,session);
        session.AddComponent<AccountCheckOutTimeComponent, string>(request.Account);
        
        // 生成Token（轻量级，性能优化）
        string token = TokenHelper.GenerateToken(request.Account, session.Zone());
        
        // 保留TokenComponent用于快速验证
        var tokenComponent = session.Root().GetComponent<TokenComponent>();
        tokenComponent.Remove(request.Account);
        tokenComponent.Add(request.Account, token);
        
        response.Token = token;
        response.Error = ErrorCode.ERR_Success;
        await ETTask.CompletedTask;
    }
}