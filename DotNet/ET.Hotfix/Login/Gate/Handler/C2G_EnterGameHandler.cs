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

        var sessionPlayerComponent = session.GetComponent<SessionPlayerComponent>();
        if (sessionPlayerComponent.Player == null)
        {
            response.Error = ErrorCode.ERR_SessionPlayerError;
            return;
        }
        var player = sessionPlayerComponent.Player;
        if (player == null || player.IsDisposed)
        {
            response.Error = ErrorCode.ERR_NonePlayerError;
            return;
        }
        
        CoroutineLockComponent coroutineLockComponent = session.Root().GetComponent<CoroutineLockComponent>();

        var instanceId = session.InstanceId;
        using (session.AddComponent<SessionLockingComponent>())
        {
            using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate,player.Account.GetLongHashCode()))
            {
                if (session.InstanceId != instanceId || player.IsDisposed)
                {
                    response.Error = ErrorCode.ERR_SessionPlayerError;
                    return;
                }

                if (player.PlayerState == PlayerState.Game)
                {
                    G2M_SecondLogin g2MSecondLogin = G2M_SecondLogin.Create();
                    var m2GSecondLogin = (M2G_SecondLogin) await session.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit)
                        .Call(player.PlayerId, g2MSecondLogin);
                    
                    if(m2GSecondLogin.Error == ErrorCode.ERR_Success)
                    {
                        response.Error = ErrorCode.ERR_Success;
                        Log.Info("补全二次登录逻辑！");
                        return;
                    }
                    Log.Error("二次登录失败");
                    response.Error = ErrorCode.ERR_ReEnterGameError;
                    await DisconnectHelper.KickPlayerNoLock(player);
                    session.Disconnect().NoContext();
                }

                try
                {
                    // 在Gate上动态创建一个Map Scene，把Unit从DB中加载放进来，然后传送到真正的Map中，这样登陆跟传送的逻辑就完全一样了
                    GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
                    gateMapComponent.Scene = await GateMapFactory.Create(gateMapComponent, player.Id, GenerateIdManager.Instance.GenerateInstanceId(), "GateMap");

                    Scene scene = gateMapComponent.Scene;
                    // 这里可以从DB中加载Unit
                    Unit unit = UnitFactory.Create(scene, player.Id, UnitType.Player);
          
                    StartSceneTable startSceneConfig = StartSceneConfigManager.Instance.GetBySceneName(session.Zone(), "Map1");
                    Log.Debug("C2G_EnterGameHandler");
                    // 等到一帧的最后面再传送，先让G2C_EnterMap返回，否则传送消息可能比G2C_EnterMap还早
                    TransferHelper.TransferAtFrameFinish(unit, startSceneConfig.ActorId, startSceneConfig.Name).NoContext();
                    response.MyUnitId = unit.Id;
                    player.PlayerId = unit.Id;
                    player.PlayerState = PlayerState.Game;
                }
                catch (Exception e)
                {
                    Log.Error($"角色进入游戏逻辑服出问题：{player.Account} {e.Message}");
                    response.Error = ErrorCode.ERR_EnterGameError;
                    await DisconnectHelper.KickPlayerNoLock(player);
                    session.Disconnect().NoContext();
                }
                
            }
        }
    }
}