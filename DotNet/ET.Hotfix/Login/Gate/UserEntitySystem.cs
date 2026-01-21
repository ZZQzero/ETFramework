namespace ET
{
    [EntitySystemOf(typeof(UserEntity))]
    public static partial class UserEntitySystem
    {
        [EntitySystem]
        private static void Awake(this UserEntity self, string account, long roleId)
        {
            self.Account = account;
            self.CurrentRoleId = roleId;
        }
    }
}