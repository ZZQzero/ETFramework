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

                var userEntityComponent = root.GetComponent<UserEntityComponent>();
                var userEntity = userEntityComponent.GetByAccount(request.AccountName);
                if (userEntity == null)
                {
                    // 使用RoleId作为Player的Id（保持现有逻辑），但同时存储UserId
                    userEntity = userEntityComponent.AddChildWithId<UserEntity, string, long>(request.UserId, request.AccountName, request.UserId);
                    userEntity.CurrentRoleId = request.RoleId;
                    userEntityComponent.Add(userEntity);

                    UserEntitySessionComponent userEntitySessionComponent = userEntity.AddComponent<UserEntitySessionComponent>();
                    userEntitySessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                    userEntity.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);

                    List<(int type, long key, ActorId actorId)> batchItems =
                        new List<(int type, long key, ActorId actorId)>
                        {
                            (LocationType.GateSession, userEntity.UserId, userEntitySessionComponent.GetActorId()),
                            (LocationType.User, userEntity.UserId, userEntity.GetActorId())
                        };
                    await root.GetComponent<LocationProxyComponent>().AddBatchLocation(batchItems);

                    session.AddComponent<SessionUserEntityComponent>().UserEntity = userEntity;
                    userEntitySessionComponent.Session = session;
                    userEntity.State = UserSessionState.Gate;
                }
                else
                {
                    userEntity.RemoveComponent<UserOfflineOutTimeComponent>();
                    session.AddComponent<SessionUserEntityComponent>().UserEntity = userEntity;
                    
                    // 如果 PlayerSessionComponent 不存在（可能被顶号时移除了），需要重新创建
                    UserEntitySessionComponent userEntitySessionComponent = userEntity.GetComponent<UserEntitySessionComponent>();
                    if (userEntitySessionComponent == null)
                    {
                        userEntitySessionComponent = userEntity.AddComponent<UserEntitySessionComponent>();
                        userEntitySessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                        
                        // 重新注册 Location
                        List<(int type, long key, ActorId actorId)> batchItems =
                            new List<(int type, long key, ActorId actorId)>
                            {
                                (LocationType.GateSession, userEntity.UserId, userEntitySessionComponent.GetActorId()),
                                (LocationType.User, userEntity.UserId, userEntity.GetActorId())
                            };
                        await root.GetComponent<LocationProxyComponent>().AddBatchLocation(batchItems);
                    }
                    
                    userEntitySessionComponent.Session = session;
                }
            }
        }
    }
    
}