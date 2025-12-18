namespace ET;

public static partial class TokenComponentSystem
{
    [EntitySystem]
    private static void Awake(this TokenComponent self)
    {
        
    }

    public static void Add(this TokenComponent self, long userId,string token)
    {
        self.TokenDic.Add(userId,token);
        self.TimeOutRemoveKey(userId,token).NoContext();
    }
    
    public static void Remove(this TokenComponent self,long userId)
    {
        self.TokenDic.Remove(userId);
    }

    public static string Get(this TokenComponent self, long userId)
    {
        return self.TokenDic.GetValueOrDefault(userId);
    }

    private static async ETTask TimeOutRemoveKey(this TokenComponent self,long userId,string token)
    {
        await self.Root().GetComponent<TimerComponent>().WaitAsync(600000);
        var onlineToken = self.Get(userId);
        if (!string.IsNullOrEmpty(onlineToken) && onlineToken == token)
        {
            self.Remove(userId);
        }
    }
}