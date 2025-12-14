namespace ET;

[Invoke(TimerInvokeType.PlayerOfflineOutTimer)]
public class PlayerOfflineOurTime : ATimer<PlayerOfflineOutTimeComponent>
{
    protected override void Run(PlayerOfflineOutTimeComponent t)
    {
        t.KickPlayer();
    }
}

public static partial class PlayerOfflineOutTimeComponentSystem
{
    [EntitySystem]
    private static void Awake(this PlayerOfflineOutTimeComponent self)
    {
        Log.Debug("添加离线保护PlayerOfflineOutTimeComponent");
        self.Timer = self.Root().GetComponent<TimerComponent>().NewOnceTimer(TimeInfo.Instance.ServerNow() + 10000,
            TimerInvokeType.PlayerOfflineOutTimer,self);
    }

    [EntitySystem]
    private static void Destroy(this PlayerOfflineOutTimeComponent self)
    {
        self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
    }

    public static void KickPlayer(this PlayerOfflineOutTimeComponent self)
    {
        DisconnectHelper.KickPlayer(self.GetParent<Player>()).NoContext();
    }
}