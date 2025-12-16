namespace ET;

[MessageHandler(SceneType.Gate)]
public class L2G_DisconnectGateUnitHandler : MessageHandler<Scene,L2G_DisconnectGateUnit,G2L_DisconnectGateUnit>
{
    protected override async ETTask Run(Scene scene, L2G_DisconnectGateUnit request, G2L_DisconnectGateUnit response)
    {
        CoroutineLockComponent coroutineLockComponent = scene.GetComponent<CoroutineLockComponent>();
        using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, request.AccountName.GetLongHashCode()))
        {
            UserEntityComponent userEntityComponent = scene.GetComponent<UserEntityComponent>();
            UserEntity userEntity = userEntityComponent.GetByAccount(request.AccountName);
            if (userEntity == null)
            {
                return;
            }

            scene.GetComponent<GateSessionKeyComponent>().Remove(request.AccountName.GetLongHashCode());
            
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
            
            await userEntity.RemoveLocation(LocationType.User);

            userEntity.AddComponent<UserOfflineOutTimeComponent>();
        }
           
    }
}