namespace ET
{
    [EntitySystemOf(typeof(UserEntity))]
    public static partial class UserEntitySystem
    {
        [EntitySystem]
        private static void Awake(this UserEntity self, string account, long userId)
        {
            self.Account = account;
            self.UserId = userId;
        }
    }
}