using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public enum AccountType
    {
        General = 0,
        BlackList,
    }
    
    public class Account
    {
        [BsonId]
        public string AccountName;
        public long UserId;
        public string Password;
        public long CreateTime;
        public AccountType AccountType;
    }
}