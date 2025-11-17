namespace ET;

public class AccountSessionComponent : Entity,IAwake, IDestroy
{
    public Dictionary<string,EntityRef<Session>> AccountSessionDic = new Dictionary<string,EntityRef<Session>>();
}
