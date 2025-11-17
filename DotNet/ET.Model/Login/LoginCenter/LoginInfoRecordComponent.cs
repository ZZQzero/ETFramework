namespace ET;

public class LoginInfoRecordComponent : Entity,IAwake,IDestroy
{
    public Dictionary<long,int> LoginAccountInfo = new Dictionary<long,int>();
}