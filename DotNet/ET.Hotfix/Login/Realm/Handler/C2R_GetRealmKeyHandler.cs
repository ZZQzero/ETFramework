namespace ET;

[MessageSessionHandler(SceneType.Realm)]
public class C2R_GetRealmKeyHandler : MessageSessionHandler<C2R_GetRealmKey,R2C_GetRealmKey>
{
    protected override async ETTask Run(Session session, C2R_GetRealmKey request, R2C_GetRealmKey response)
    {
        var userId = request.UserId;
        if (session.GetComponent<SessionLockingComponent>() != null)
        {
            response.Error = ErrorCode.ERR_RequestRepeatedly;
            session?.Disconnect().NoContext();
            return;
        }
        
        var tokenComponent = session.Root().GetComponent<TokenComponent>();
        string cachedToken = tokenComponent.Get(userId);
        
        if (cachedToken != request.Token)
        {
            Log.Warning($"Token验证失败：用户 {userId}");
            response.Error = ErrorCode.ERR_TokenError;
            session?.Disconnect().NoContext();
            return;
        }
        tokenComponent.Remove(userId);
        var coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginAccount, userId))
            {
                var config = RealmGateAddressHelper.GetGate(request.ServerId, userId);
                
                // 向gate请求一个key,客户端可以拿着这个key连接gate
                R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
                r2GGetLoginKey.UserId = userId;
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