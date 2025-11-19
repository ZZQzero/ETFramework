namespace ET;

[MessageSessionHandler(SceneType.Realm)]
public class C2R_GetRealmKeyHandler : MessageSessionHandler<C2R_GetRealmKey,R2C_GetRealmKey>
{
    protected override async ETTask Run(Session session, C2R_GetRealmKey request, R2C_GetRealmKey response)
    {
        if (session.GetComponent<SessionLockingComponent>() != null)
        {
            response.Error = ErrorCode.ERR_RequestRepeatedly;
            session?.Disconnect().NoContext();
            return;
        }

        // 验证Token（优先使用TokenComponent快速验证，失败则使用签名验证）
        string token = request.Token;
        var tokenComponent = session.Root().GetComponent<TokenComponent>();
        string cachedToken = tokenComponent.Get(request.Account);
        
        bool tokenValid = false;
        if (cachedToken == token)
        {
            // 快速路径：TokenComponent中匹配，直接通过
            tokenValid = true;
        }
        else if (TokenHelper.ValidateToken(token, out string account, out int zone))
        {
            // 慢速路径：签名验证，并检查账号匹配
            if (account == request.Account)
            {
                tokenValid = true;
            }
        }
        
        if (!tokenValid)
        {
            response.Error = ErrorCode.ERR_TokenError;
            session?.Disconnect().NoContext();
            return;
        }

        var coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount, request.Account.GetLongHashCode()))
            {
                var config = RealmGateAddressHelper.GetGate(request.ServerId, request.Account);
                
                // 向gate请求一个key,客户端可以拿着这个key连接gate
                R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
                r2GGetLoginKey.Account = request.Account;
                G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await session.Root().GetComponent<MessageSender>().Call(
                    config.ActorId, r2GGetLoginKey);

                response.Address = config.InnerIPPort.ToString();
                response.Key = g2RGetLoginKey.Key;
                response.GateId = g2RGetLoginKey.GateId;
                session.Disconnect().NoContext();
            }
        }
    }
}