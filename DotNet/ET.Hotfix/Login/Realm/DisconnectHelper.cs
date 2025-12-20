namespace ET;

public static class DisconnectHelper
{
    public static async ETTask Disconnect(this Session self)
    {
        if (self == null || self.IsDisposed)
        {
            return;
        }

        long instanceId = self.InstanceId;

        TimerComponent timerComponent = self.Root().GetComponent<TimerComponent>();
        await timerComponent.WaitAsync(1000);

        if (self.InstanceId != instanceId)
        {
            return;
        }

        self.Dispose();
    }

    public static async ETTask KickUser(UserEntity userEntity)
    {
        if (userEntity == null || userEntity.IsDisposed)
        {
            return;
        }

        long instanceId = userEntity.InstanceId;
        CoroutineLockComponent coroutineLockComponent = userEntity.Root().GetComponent<CoroutineLockComponent>();
        using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, userEntity.UserId))
        {
            if (userEntity.IsDisposed || userEntity.InstanceId != instanceId)
            {
                return;
            }
            await KickUserNoLock(userEntity);
        }
    }

    public static async ETTask KickUserNoLock(UserEntity userEntity)
    {
        if (userEntity == null || userEntity.IsDisposed)
        {
            return;
        }
        var locationSenderComponent = userEntity.Root().GetComponent<MessageLocationSenderComponent>();

        switch (userEntity.State)
        {
            case UserSessionState.Disconnect:
                break;
            case UserSessionState.Gate:
                break;
            case UserSessionState.Game:
                var m2GRequestExitGame = (M2G_RequestExitGame)await locationSenderComponent
                    .Get(LocationType.Unit).Call(userEntity.UserId, G2M_RequestExitGame.Create());
                
                G2L_RemoveLoginRecord g2LRemoveLoginRecord = G2L_RemoveLoginRecord.Create();
                g2LRemoveLoginRecord.AccountName = userEntity.Account;
                g2LRemoveLoginRecord.ServerId = userEntity.Zone();
                var l2GRemoveLoginRecord = (L2G_RemoveLoginRecord)await userEntity.Root().GetComponent<MessageSender>()
                    .Call(StartSceneConfigManager.Instance.LoginCenterActorId, g2LRemoveLoginRecord);
                break;
        }

        TimerComponent timerComponent = userEntity.Root().GetComponent<TimerComponent>();
        userEntity.State = UserSessionState.Disconnect;
        locationSenderComponent.Get(LocationType.GateSession).Remove(userEntity.UserId);
        
        await userEntity.RemoveLocation(LocationType.User);
        
        UserEntitySessionComponent userEntitySessionComponent = userEntity.GetComponent<UserEntitySessionComponent>();
        if (userEntitySessionComponent != null)
        {
            await userEntitySessionComponent.RemoveLocation(LocationType.GateSession);
        }
        
        userEntity.Root().GetComponent<UserEntityComponent>()?.Remove(userEntity);
        userEntity?.Dispose();
        await timerComponent.WaitAsync(300);
    }

}