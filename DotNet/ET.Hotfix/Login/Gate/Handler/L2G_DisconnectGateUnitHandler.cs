namespace ET;

[MessageHandler(SceneType.Gate)]
public class L2G_DisconnectGateUnitHandler : MessageHandler<Scene,L2G_DisconnectGateUnit,G2L_DisconnectGateUnit>
{
    protected override async ETTask Run(Scene scene, L2G_DisconnectGateUnit request, G2L_DisconnectGateUnit response)
    {
        CoroutineLockComponent coroutineLockComponent = scene.GetComponent<CoroutineLockComponent>();
        using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, request.AccountName.GetLongHashCode()))
        {
            PlayerComponent playerComponent = scene.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(request.AccountName);
            if (player == null)
            {
                return;
            }

            scene.GetComponent<GateSessionKeyComponent>().Remove(request.AccountName.GetLongHashCode());
            
            PlayerSessionComponent playerSessionComponent = player.GetComponent<PlayerSessionComponent>();
            if (playerSessionComponent != null)
            {
                Session gateSession = playerSessionComponent.Session;
                if (gateSession != null && !gateSession.IsDisposed)
                {
                    Log.Debug($"通知另一个客户端下线 {gateSession.Id}");
                    A2C_Disconnect a2CDisconnect = A2C_Disconnect.Create();
                    a2CDisconnect.Error = ErrorCode.ERR_OtherAccountLogin;
                    gateSession.Send(a2CDisconnect);
                    gateSession.Disconnect().NoContext();
                }
                
                // 先移除 Location 注册，避免内存泄漏
                // 关键：PlayerSessionComponent 的 Id 是全局唯一的，每次创建都是新 Id
                // 如果新客户端在10秒内登录，PlayerOfflineOutTimeComponent 会被移除，KickPlayer 不会执行
                // 所以旧的 GateSession Location 永远不会被清理，必须在移除组件前清理
                await playerSessionComponent.RemoveLocation(LocationType.GateSession);
                
                // 移除组件（会自动清理 EntityRef）
                player.RemoveComponent<PlayerSessionComponent>();
            }
            
            // 移除 Player 的 Location，确保无论新客户端何时登录，旧的 Location 都会被清理
            // 虽然新客户端登录时会重新注册（覆盖），但提前清理可以避免旧 ActorId 指向已销毁的 Entity
            await player.RemoveLocation(LocationType.Player);

            player.AddComponent<PlayerOfflineOutTimeComponent>();
        }
           
    }
}