namespace ET;

[MessageSessionHandler(SceneType.Gate)]
public class C2G_EnterGameHandler : MessageSessionHandler<C2G_EnterGame,G2C_EnterGame>
{
    protected override async ETTask Run(Session session, C2G_EnterGame request, G2C_EnterGame response)
    {
        if (session.GetComponent<SessionLockingComponent>() != null)
        {
            response.Error = ErrorCode.ERR_RequestRepeatedly;
            return;
        }

        var sessionPlayerComponent = session.GetComponent<SessionUserEntityComponent>();
        if (sessionPlayerComponent.UserEntity == null)
        {
            response.Error = ErrorCode.ERR_SessionPlayerError;
            return;
        }
        var userEntity = sessionPlayerComponent.UserEntity;
        if (userEntity == null || userEntity.IsDisposed)
        {
            response.Error = ErrorCode.ERR_NonePlayerError;
            return;
        }
        
        CoroutineLockComponent coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();

        var instanceId = session.InstanceId;
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, userEntity.Account.GetLongHashCode()))
            {
                if (session.InstanceId != instanceId || userEntity.IsDisposed)
                {
                    response.Error = ErrorCode.ERR_SessionPlayerError;
                    return;
                }

                if (userEntity.State == UserSessionState.Game)
                {
                    // 二次登录：账号被顶号后重新登录，Unit已经在Map服务器中
                    // 流程：
                    // 1. 更新Gate服务器的Session绑定和Location注册
                    // 2. 通知Map服务器（通过RPC保证时序）
                    // 3. Map服务器清理缓存并发送消息给客户端
                    
                    // 先更新Session绑定
                    SessionUserEntityComponent sessionUserEntityComp = session.GetComponent<SessionUserEntityComponent>();
                    if (sessionUserEntityComp == null)
                    {
                        sessionUserEntityComp = session.AddComponent<SessionUserEntityComponent>();
                    }
                    sessionUserEntityComp.UserEntity = userEntity;
                    
                    // 更新PlayerSessionComponent的Session引用
                    UserEntitySessionComponent userEntitySessionComponent = userEntity.GetComponent<UserEntitySessionComponent>();
                    if (userEntitySessionComponent == null)
                    {
                        // 如果PlayerSessionComponent不存在，需要重新创建
                        userEntitySessionComponent = userEntity.AddComponent<UserEntitySessionComponent>();
                        userEntitySessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                    }
                    userEntitySessionComponent.Session = session;
                    
                    // 重新注册Location（关键步骤）
                    List<(int type, long key, ActorId actorId)> locationBatchItems =
                        new List<(int type, long key, ActorId actorId)>
                        {
                            (LocationType.GateSession, userEntity.UserId, userEntitySessionComponent.GetActorId()),
                            (LocationType.User, userEntity.Id, userEntity.GetActorId())
                        };
                    await session.Root().GetComponent<LocationProxyComponent>().AddBatchLocation(locationBatchItems);
                    
                    G2M_SecondLogin g2MSecondLogin = G2M_SecondLogin.Create();
                    var locationSend = session.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit);
                    var m2GSecondLogin = (M2G_SecondLogin) await locationSend.Call(userEntity.UserId, g2MSecondLogin);
                    
                    if (m2GSecondLogin.Error == ErrorCode.ERR_Success)
                    {
                        response.Error = ErrorCode.ERR_Success;
                        Log.Info($"二次登录成功：账号 {userEntity.Account}, UserId={userEntity.UserId}");
                        return;
                    }
                    
                    Log.Error($"二次登录失败：账号 {userEntity.Account}");
                    response.Error = ErrorCode.ERR_ReEnterGameError;
                    await DisconnectHelper.KickUserNoLock(userEntity);
                    session.Disconnect().NoContext();
                }

                try
                {
                    // 直接通知Map服务器创建Unit，不需要在Gate创建临时场景和Unit
                    StartSceneTable startSceneConfig = StartSceneConfigManager.Instance.GetBySceneName(session.Zone(), "Map1");
                    long unitId = userEntity.UserId; // 用UserId作为UnitId，确保全局唯一

                    // 先设置GateSession的Location，以便Map服务器发送消息
                    UserEntitySessionComponent userEntitySessionComponent = userEntity.GetComponent<UserEntitySessionComponent>();
                    if (userEntitySessionComponent == null)
                    {
                        userEntitySessionComponent = userEntity.AddComponent<UserEntitySessionComponent>();
                        userEntitySessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                    }
                    userEntitySessionComponent.Session = session;

                    // 设置GateSession Location，key为UnitId
                    await session.Root().GetComponent<LocationProxyComponent>().Add(LocationType.GateSession, unitId, userEntitySessionComponent.GetActorId());

                    // 发送创建Unit请求到Map服务器（首次创建，不需要Lock）
                    M2M_UnitTransferRequest m2MUnitTransferRequest = M2M_UnitTransferRequest.Create();
                    m2MUnitTransferRequest.OldActorId = default; // 首次创建，没有旧的ActorId
                    m2MUnitTransferRequest.UnitInfo = UnitInfo.Create();
                    m2MUnitTransferRequest.UnitInfo.ConfigId = 1001; // 默认配置ID，可以从数据库加载
                    m2MUnitTransferRequest.UnitInfo.UnitId = unitId;

                    M2M_UnitTransferResponse m2MUnitTransferResponse = (M2M_UnitTransferResponse)await session.Root().GetComponent<MessageSender>().Call(startSceneConfig.ActorId, m2MUnitTransferRequest);

                    if (m2MUnitTransferResponse.Error != ErrorCode.ERR_Success)
                    {
                        Log.Error($"创建Unit失败：{m2MUnitTransferResponse.Message}");
                        response.Error = ErrorCode.ERR_EnterGameError;
                        await DisconnectHelper.KickUserNoLock(userEntity);
                        session.Disconnect().NoContext();
                        return;
                    }

                    userEntity.State = UserSessionState.Game;
                }
                catch (Exception e)
                {
                    Log.Error($"角色进入游戏逻辑服出问题：{userEntity.Account} {e.Message}");
                    response.Error = ErrorCode.ERR_EnterGameError;
                    await DisconnectHelper.KickUserNoLock(userEntity);
                    session.Disconnect().NoContext();
                }
                
            }
        }
    }
}