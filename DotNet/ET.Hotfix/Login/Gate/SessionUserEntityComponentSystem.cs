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
                return;
            }
            
            DisconnectHelper.KickUser(userEntity).NoContext();
        }
        
        [EntitySystem]
        private static void Awake(this SessionUserEntityComponent self)
        {

        }
    }
}