namespace ET;

public class LoginInfoRecordComponent : Entity,IAwake,IDestroy
{
    public readonly Dictionary<long,int> LoginAccountInfo = new Dictionary<long,int>();
}