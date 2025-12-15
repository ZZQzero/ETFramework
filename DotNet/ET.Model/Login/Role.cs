using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 角色数据实体（持久化到MongoDB）
    /// </summary>
    public class Role
    {
        [BsonId]
        public long Id;
        
        public string RoleName;
        
        public int Level;
        
        public int Job;
        
        public long CreateTime;
    }
}