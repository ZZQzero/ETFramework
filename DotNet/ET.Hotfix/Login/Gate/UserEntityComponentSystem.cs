using System.Linq;

namespace ET
{
    [FriendOf(typeof(UserEntityComponent))]
    public static partial class UserEntityComponentSystem
    {
        public static void Add(this UserEntityComponent self, UserEntity userEntity)
        {
            self.dictionary.Add(userEntity.UserId, userEntity);
        }
        
        public static void Remove(this UserEntityComponent self, UserEntity userEntity)
        {
            self.dictionary.Remove(userEntity.UserId);
            userEntity.Dispose();
        }
        
        public static UserEntity GetByAccount(this UserEntityComponent self, long userId)
        {
            self.dictionary.TryGetValue(userId, out EntityRef<UserEntity> player);
            return player;
        }
    }
}