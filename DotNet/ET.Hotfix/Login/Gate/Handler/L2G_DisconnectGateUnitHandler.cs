namespace ET;

[MessageHandler(SceneType.Gate)]
public class L2G_DisconnectGateUnitHandler : MessageHandler<Scene,L2G_DisconnectGateUnit,G2L_DisconnectGateUnit>
{
    protected override async ETTask Run(Scene scene, L2G_DisconnectGateUnit request, G2L_DisconnectGateUnit response)
    {
        CoroutineLockComponent coroutineLockComponent = scene.GetComponent<CoroutineLockComponent>();
        var userId = request.UserId;
        using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, userId))
        {
            UserEntityComponent userEntityComponent = scene.GetComponent<UserEntityComponent>();
            UserEntity userEntity = userEntityComponent.GetByAccount(userId);
            if (userEntity == null)
            {
                return;
            }

            scene.GetComponent<GateSessionKeyComponent>().Remove(userId);
            
            UserEntitySessionComponent userEntitySessionComponent = userEntity.GetComponent<UserEntitySessionComponent>();
            if (userEntitySessionComponent != null)
            {
                Session gateSession = userEntitySessionComponent.Session;
                if (gateSession != null && !gateSession.IsDisposed)
                {
                    Log.Debug($"通知另一个客户端下线 {gateSession.Id}");
                    A2C_Disconnect a2CDisconnect = A2C_Disconnect.Create();
                    a2CDisconnect.Error = ErrorCode.ERR_OtherAccountLogin;
                    gateSession.Send(a2CDisconnect);
                    gateSession.Disconnect().NoContext();
                }
                
                await userEntitySessionComponent.RemoveLocation(LocationType.GateSession);
                userEntity.RemoveComponent<UserEntitySessionComponent>();
            }
            userEntity.AddComponent<UserOfflineOutTimeComponent>();
        }
           
    }
}