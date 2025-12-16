namespace ET;

[Invoke(TimerInvokeType.PlayerOfflineOutTimer)]
public class UserOfflineOurTime : ATimer<UserOfflineOutTimeComponent>
{
    protected override void Run(UserOfflineOutTimeComponent t)
    {
        t.KickPlayer();
    }
}

public static partial class UserOfflineOutTimeComponentSystem
{
    [EntitySystem]
    private static void Awake(this UserOfflineOutTimeComponent self)
    {
        Log.Debug("添加离线保护PlayerOfflineOutTimeComponent");
        self.Timer = self.Root().GetComponent<TimerComponent>().NewOnceTimer(TimeInfo.Instance.ServerNow() + 10000,
            TimerInvokeType.PlayerOfflineOutTimer,self);
    }

    [EntitySystem]
    private static void Destroy(this UserOfflineOutTimeComponent self)
    {
        self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
    }

    public static void KickPlayer(this UserOfflineOutTimeComponent self)
    {
        DisconnectHelper.KickUser(self.GetParent<UserEntity>()).NoContext();
    }
}