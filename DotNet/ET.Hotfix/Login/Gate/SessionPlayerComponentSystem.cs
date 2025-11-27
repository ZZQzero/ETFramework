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
            
            // Session断开时，根据不同场景处理：
            // 1. 如果Player已经有PlayerOfflineOutTimeComponent，说明是离线保护期，不需要重复处理
            // 2. 如果Player在游戏中（PlayerState.Game），触发完整的KickPlayer流程
            // 3. 其他情况（Gate、Disconnect），简单清理即可
            
            if (player.GetComponent<PlayerOfflineOutTimeComponent>() != null)
            {
                // 已经在离线保护期，不需要重复处理
                Log.Info($"Player {player.Account} 已在离线保护期，跳过Session断开处理");
                return;
            }
            DisconnectHelper.KickPlayer(player).NoContext();
            
            Log.Info($"Session断开，触发Player {player.Account} 的清理流程，PlayerState: {player.PlayerState}");
        }
        
        [EntitySystem]
        private static void Awake(this SessionPlayerComponent self)
        {

        }
    }
}