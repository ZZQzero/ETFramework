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
                
                await playerSessionComponent.RemoveLocation(LocationType.GateSession);
                player.RemoveComponent<PlayerSessionComponent>();
            }
            
            await player.RemoveLocation(LocationType.Player);

            player.AddComponent<PlayerOfflineOutTimeComponent>();
        }
           
    }
}