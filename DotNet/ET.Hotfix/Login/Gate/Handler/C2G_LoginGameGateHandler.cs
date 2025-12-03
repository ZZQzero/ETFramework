namespace ET;

[MessageSessionHandler(SceneType.Gate)]
public class C2G_LoginGameGateHandler : MessageSessionHandler<C2G_LoginGameGate,G2C_LoginGameGate>
{
    protected override async ETTask Run(Session session, C2G_LoginGameGate request, G2C_LoginGameGate response)
    {
        var root = session.Root();
        var sessionLock = session.GetComponent<SessionLockingComponent>();
        if (sessionLock != null)
        {
            response.Error = ErrorCode.ERR_RequestRepeatedly;
            session?.Disconnect().NoContext();
            return;
        }

        var gateSession = root.GetComponent<GateSessionKeyComponent>();
        var account = gateSession.Get(request.Key);
        if (string.IsNullOrEmpty(account))
        {
            response.Error = ErrorCode.ERR_ConnectGateKeyError;
            session?.Disconnect().NoContext();
            return;
        }
        gateSession.Remove(request.Key);
        session.RemoveComponent<SessionAcceptTimeoutComponent>();
        var coroutineLock = root.GetComponent<CoroutineLockComponent>();

        var instanceId = session.InstanceId;
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLock.Wait(CoroutineLockType.LoginGate, request.AccountName.GetLongHashCode()))
            {
                if (instanceId != session.InstanceId)
                {
                    response.Error = ErrorCode.ERR_LoginGateGameError;
                    session?.Disconnect().NoContext();
                    return;
                }
                G2L_AddLoginRecord g2LAddLoginRecord = G2L_AddLoginRecord.Create();
                g2LAddLoginRecord.AccountName = request.AccountName;
                g2LAddLoginRecord.ServerId = root.Zone();
                
                var config = StartSceneConfig.Instance.GetOrDefault(202);
                var l2GAddLoginRecord = (L2G_AddLoginRecord) await session.Root().GetComponent<MessageSender>().Call(config.ActorId, g2LAddLoginRecord);

                if (l2GAddLoginRecord.Error != ErrorCode.ERR_Success)
                {
                    response.Error = l2GAddLoginRecord.Error;
                    session?.Disconnect().NoContext();
                    return;
                }

                var playerComponent = root.GetComponent<PlayerComponent>();
                var player = playerComponent.GetByAccount(request.AccountName);
                if (player == null)
                {
                    player = playerComponent.AddChildWithId<Player, string>(request.RoleId, request.AccountName);
                    player.PlayerId = request.RoleId;
                    playerComponent.Add(player);

                    PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
                    playerSessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                    player.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);

                    List<(int type, long key, ActorId actorId)> batchItems =
                        new List<(int type, long key, ActorId actorId)>
                        {
                            (LocationType.GateSession, playerSessionComponent.Id, playerSessionComponent.GetActorId()),
                            (LocationType.Player, player.Id, player.GetActorId())
                        };
                    await root.GetComponent<LocationProxyComponent>().AddBatchLocation(batchItems);

                    session.AddComponent<SessionPlayerComponent>().Player = player;
                    playerSessionComponent.Session = session;
                    player.PlayerState = PlayerState.Gate;
                }
                else
                {
                    player.RemoveComponent<PlayerOfflineOutTimeComponent>();
                    session.AddComponent<SessionPlayerComponent>().Player = player;
                    player.GetComponent<PlayerSessionComponent>().Session = session;
                }
                response.PlayerId = player.PlayerId;
            }
        }
    }
    
}