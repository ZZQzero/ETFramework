namespace ET;

public class AccountCheckOutTimeComponent : Entity,IAwake<string>,IDestroy
{
    public long Timer;
    public string Account;
}