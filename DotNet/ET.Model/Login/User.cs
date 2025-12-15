using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public enum AccountType
    {
        Phone = 0,
        Email = 1,
        WeChat = 2,
        Guest = 3,
    }
    
    public enum UserStatus
    {
        Normal = 0,
        Banned = 1,
    }
    
    /// <summary>
    /// 用户资料（扩展信息）
    /// </summary>
    public class UserProfile
    {
        public int VipLevel;
        
        public long TotalRecharge;
        
        public long LastLoginTime;
    }
    
    /// <summary>
    /// 用户账号数据（持久化到MongoDB）
    /// </summary>
    public class User
    {
        /// <summary>登录账号（手机号/邮箱/微信等，唯一且不可变）</summary>
        [BsonId]
        public string Account;
        
        /// <summary>账号类型</summary>
        public AccountType AccountType;
        
        /// <summary>用户ID（永久唯一标识，用于好友、排行榜等）</summary>
        public long UserId;
        
        /// <summary>用户名/昵称（可修改，用于游戏内显示）</summary>
        public string Username;
        
        /// <summary>密码（哈希值）</summary>
        public string Password;
        
        /// <summary>创建时间</summary>
        public long CreateTime;
        
        /// <summary>用户状态</summary>
        public UserStatus Status;
        
        /// <summary>角色ID列表</summary>
        public List<long> RoleIds;
        
        /// <summary>用户资料</summary>
        public UserProfile Profile;
    }
}