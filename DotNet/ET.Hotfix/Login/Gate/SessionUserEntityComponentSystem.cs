namespace ET
{
    [EntitySystemOf(typeof(SessionUserEntityComponent))]
    public static partial class SessionUserEntityComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this SessionUserEntityComponent self)
        {
            Scene root = self.Root();
            if (root.IsDisposed)
            {
                return;
            }
            
            UserEntity userEntity = self.UserEntity;
            if (userEntity == null || userEntity.IsDisposed)
            {
                return;
            }
            
            if (userEntity.GetComponent<UserOfflineOutTimeComponent>() != null)
            {
                Log.Info($"Player {userEntity.Account} 已在离线保护期");
                return;
            }
            
            DisconnectHelper.KickUser(userEntity).NoContext();
            Log.Info($"Session断开，触发Player {userEntity.Account} 清理流程");
        }
        
        [EntitySystem]
        private static void Awake(this SessionUserEntityComponent self)
        {

        }
    }
}