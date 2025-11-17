namespace ET
{
    public enum AccountType
    {
        General = 0,
        BlackList,
    }
    
    public class Account
    {
        public long UserId;
        public string AccountName;
        public string Password;
        public long CreateTime;
        public AccountType AccountType;
    }
}