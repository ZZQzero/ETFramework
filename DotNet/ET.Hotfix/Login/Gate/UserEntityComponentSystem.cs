using System.Linq;

namespace ET
{
    [FriendOf(typeof(UserEntityComponent))]
    public static partial class UserEntityComponentSystem
    {
        public static void Add(this UserEntityComponent self, UserEntity userEntity)
        {
            self.dictionary.Add(userEntity.Account, userEntity);
        }
        
        public static void Remove(this UserEntityComponent self, UserEntity userEntity)
        {
            self.dictionary.Remove(userEntity.Account);
            userEntity.Dispose();
        }
        
        public static UserEntity GetByAccount(this UserEntityComponent self, string account)
        {
            self.dictionary.TryGetValue(account, out EntityRef<UserEntity> player);
            return player;
        }
    }
}