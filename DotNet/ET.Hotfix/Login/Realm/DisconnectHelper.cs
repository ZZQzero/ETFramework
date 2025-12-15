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

    public static async ETTask KickPlayer(Player player)
    {
        if (player == null || player.IsDisposed)
        {
            return;
        }

        long instanceId = player.InstanceId;
        CoroutineLockComponent coroutineLockComponent = player.Root().GetComponent<CoroutineLockComponent>();
        using (await coroutineLockComponent.Wait(CoroutineLockType.LoginGate, player.Account.GetLongHashCode()))
        {
            if (player.IsDisposed || player.InstanceId != instanceId)
            {
                return;
            }
            await KickPlayerNoLock(player);
        }
    }

    public static async ETTask KickPlayerNoLock(Player player)
    {
        if (player == null || player.IsDisposed)
        {
            return;
        }
        var locationSenderComponent = player.Root().GetComponent<MessageLocationSenderComponent>();

        switch (player.State)
        {
            case PlayerSessionState.Disconnect:
                break;
            case PlayerSessionState.Gate:
                break;
            case PlayerSessionState.Game:
                var m2GRequestExitGame = (M2G_RequestExitGame)await locationSenderComponent
                    .Get(LocationType.Unit).Call(player.CurrentRoleId, G2M_RequestExitGame.Create());
                
                G2L_RemoveLoginRecord g2LRemoveLoginRecord = G2L_RemoveLoginRecord.Create();
                g2LRemoveLoginRecord.AccountName = player.Account;
                g2LRemoveLoginRecord.ServerId = player.Zone();
                var config = StartSceneConfig.Instance.GetOrDefault(202);
                var l2GRemoveLoginRecord = (L2G_RemoveLoginRecord)await player.Root().GetComponent<MessageSender>()
                    .Call(config.ActorId, g2LRemoveLoginRecord);
                break;
        }

        TimerComponent timerComponent = player.Root().GetComponent<TimerComponent>();
        player.State = PlayerSessionState.Disconnect;
        locationSenderComponent.Get(LocationType.GateSession).Remove(player.CurrentRoleId);
        
        await player.RemoveLocation(LocationType.Player);
        
        PlayerSessionComponent playerSessionComponent = player.GetComponent<PlayerSessionComponent>();
        if (playerSessionComponent != null)
        {
            await playerSessionComponent.RemoveLocation(LocationType.GateSession);
        }
        
        player.Root().GetComponent<PlayerComponent>()?.Remove(player);
        player?.Dispose();
        await timerComponent.WaitAsync(300);
    }

}