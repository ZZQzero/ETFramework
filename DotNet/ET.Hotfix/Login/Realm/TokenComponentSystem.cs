namespace ET;

public static partial class TokenComponentSystem
{
    [EntitySystem]
    private static void Awake(this TokenComponent self)
    {
        
    }

    public static void Add(this TokenComponent self, string key,string token)
    {
        self.TokenDic.Add(key,token);
        self.TimeOutRemoveKey(key,token).NoContext();
    }
    
    public static void Remove(this TokenComponent self,string key)
    {
        self.TokenDic.Remove(key);
    }

    public static string Get(this TokenComponent self, string key)
    {
        return self.TokenDic.GetValueOrDefault(key);
    }

    public static async ETTask TimeOutRemoveKey(this TokenComponent self,string key,string token)
    {
        await self.Root().GetComponent<TimerComponent>().WaitAsync(600000);
        var onlineToken = self.Get(key);
        if (!string.IsNullOrEmpty(onlineToken) && onlineToken == token)
        {
            self.Remove(key);
        }
    }
}