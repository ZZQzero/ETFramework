namespace ET
{
    [EntitySystemOf(typeof(SessionPlayerComponent))]
    public static partial class SessionPlayerComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this SessionPlayerComponent self)
        {
            Scene root = self.Root();
            if (root.IsDisposed)
            {
                return;
            }
            
            Player player = self.Player;
            if (player == null || player.IsDisposed)
            {
                return;
            }
            
            if (player.GetComponent<PlayerOfflineOutTimeComponent>() != null)
            {
                Log.Info($"Player {player.Account} 已在离线保护期");
                return;
            }
            
            DisconnectHelper.KickPlayer(player).NoContext();
            Log.Info($"Session断开，触发Player {player.Account} 清理流程");
        }
        
        [EntitySystem]
        private static void Awake(this SessionPlayerComponent self)
        {

        }
    }
}