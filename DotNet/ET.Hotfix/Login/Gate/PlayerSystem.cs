namespace ET
{
    [EntitySystemOf(typeof(Player))]
    public static partial class PlayerSystem
    {
        [EntitySystem]
        private static void Awake(this Player self, string account, long userId)
        {
            self.Account = account;
            self.UserId = userId;
        }
    }
}