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
                            (LocationType.GateSession, userEntitySessionComponent.Id, userEntitySessionComponent.GetActorId()),
                            (LocationType.User, userEntity.Id, userEntity.GetActorId())
                        };
                    await session.Root().GetComponent<LocationProxyComponent>().AddBatchLocation(locationBatchItems);
                    
                    G2M_SecondLogin g2MSecondLogin = G2M_SecondLogin.Create();
                    var locationSend = session.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit);
                    var m2GSecondLogin = (M2G_SecondLogin) await locationSend.Call(userEntity.CurrentRoleId, g2MSecondLogin);
                    
                    if (m2GSecondLogin.Error == ErrorCode.ERR_Success)
                    {
                        response.Error = ErrorCode.ERR_Success;
                        Log.Info($"二次登录成功：账号 {userEntity.Account}, CurrentRoleId={userEntity.CurrentRoleId}");
                        return;
                    }
                    
                    Log.Error($"二次登录失败：账号 {userEntity.Account}");
                    response.Error = ErrorCode.ERR_ReEnterGameError;
                    await DisconnectHelper.KickUserNoLock(userEntity);
                    session.Disconnect().NoContext();
                }

                try
                {
                    GateMapComponent gateMapComponent = userEntity.AddComponent<GateMapComponent>();
                    gateMapComponent.Scene = await GateMapFactory.Create(gateMapComponent, userEntity.Id, GenerateIdManager.Instance.GenerateInstanceId(), "GateMap");

                    Scene scene = gateMapComponent.Scene;
                    // Unit.Id使用CurrentRoleId（角色ID），而不是player.Id（UserId）
                    Unit unit = UnitFactory.Create(scene, userEntity.CurrentRoleId, UnitType.Player);
          
                    StartSceneTable startSceneConfig = StartSceneConfigManager.Instance.GetBySceneName(session.Zone(), "Map1");
                    TransferHelper.TransferAtFrameFinish(unit, startSceneConfig.ActorId, startSceneConfig.Name).NoContext();
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