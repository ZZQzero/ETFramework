namespace ET;

public static partial class AccountSessionComponentSystem
{
    [EntitySystem]
    private static void Awake(this AccountSessionComponent self)
    {
        
    }

    [EntitySystem]
    private static void Destroy(this AccountSessionComponent self)
    {
        self.AccountSessionDic.Clear();
    }

    public static Session Get(this AccountSessionComponent self, string account)
    {
        return self.AccountSessionDic.GetValueOrDefault(account);
    }

    public static void AddAccountSession(this AccountSessionComponent self, string account, EntityRef<Session> session)
    {
        if (!self.AccountSessionDic.TryAdd(account, session))
        {
            self.AccountSessionDic[account] = session;
        }
    }
    
    public static void RemoveAccountSession(this AccountSessionComponent self, string account)
    {
        self.AccountSessionDic.Remove(account);
    }
}