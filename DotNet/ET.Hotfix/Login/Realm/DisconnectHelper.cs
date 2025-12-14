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

        switch (player.PlayerState)
        {
            case PlayerState.Disconnect:
                break;
            case PlayerState.Gate:
                break;
            case PlayerState.Game:
                //通知游戏逻辑服下线Unit角色逻辑,并将数据存入数据库
                var m2GRequestExitGame = (M2G_RequestExitGame)await locationSenderComponent
                    .Get(LocationType.Unit).Call(player.PlayerId, G2M_RequestExitGame.Create());
                //通知移除账号角色登录信息
                G2L_RemoveLoginRecord g2LRemoveLoginRecord = G2L_RemoveLoginRecord.Create();
                g2LRemoveLoginRecord.AccountName = player.Account;
                g2LRemoveLoginRecord.ServerId= player.Zone();
                var config = StartSceneConfig.Instance.GetOrDefault(202);
                var l2GRemoveLoginRecord = (L2G_RemoveLoginRecord)await player.Root().GetComponent<MessageSender>()
                    .Call(config.ActorId, g2LRemoveLoginRecord);
                break;
        }

        TimerComponent timerComponent = player.Root().GetComponent<TimerComponent>();
        player.PlayerState = PlayerState.Disconnect;
        locationSenderComponent.Get(LocationType.GateSession).Remove(player.Id);
        
        // 移除 Player 的 Location
        // 注意：如果 Location 已经被 L2G_DisconnectGateUnitHandler 移除，这里再次移除不会报错（只是无效操作）
        // 这样可以确保无论新客户端何时登录，旧的 Location 都会被清理
        await player.RemoveLocation(LocationType.Player);
        
        // 检查 PlayerSessionComponent 是否存在（可能已经被 L2G_DisconnectGateUnitHandler 移除）
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