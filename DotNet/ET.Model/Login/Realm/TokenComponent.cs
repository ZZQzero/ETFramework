namespace ET;

public class TokenComponent : Entity,IAwake
{
    public readonly Dictionary<long, string> TokenDic = new Dictionary<long, string>();
}
