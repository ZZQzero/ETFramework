namespace ET;

[Invoke(TimerInvokeType.AccountSessionCheckOutTimer)]
public class AccountSessionCheckOutTimer : ATimer<AccountCheckOutTimeComponent>
{
    protected override void Run(AccountCheckOutTimeComponent t)
    {
        t?.DeleteSession();
    }
}

public static partial class AccountCheckOutTimeComponentSystem
{
    [EntitySystem]
    private static void Awake(this AccountCheckOutTimeComponent self, string account)
    {
        self.Account = account;
        var timerComponent = self.Root().GetComponent<TimerComponent>();
        timerComponent.Remove(ref self.Timer);
        timerComponent.NewOnceTimer(TimeInfo.Instance.ServerNow() + 600000, TimerInvokeType.AccountSessionCheckOutTimer, self);
    }

    [EntitySystem]
    private static void Destroy(this AccountCheckOutTimeComponent self)
    {
        self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
    }

    public static void DeleteSession(this AccountCheckOutTimeComponent self)
    {
        var session = self.GetParent<Session>();
        var accountSessionComponent = session.Root().GetComponent<AccountSessionComponent>();
        var originSession = accountSessionComponent.Get(self.Account);
        if (originSession != null && session.InstanceId == originSession.InstanceId)
        {
            accountSessionComponent.RemoveAccountSession(self.Account);
        }
        var a2CDisconnect = A2C_Disconnect.Create();
        a2CDisconnect.Error = ErrorCode.ERR_AccountSessionCheckOutTimer;
        session?.Send(a2CDisconnect);
        session?.Disconnect().NoContext();
    }
}