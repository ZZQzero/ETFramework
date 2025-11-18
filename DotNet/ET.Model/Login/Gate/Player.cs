namespace ET
{
    public enum PlayerState
    {
        Disconnect,
        Gate,
        Game
    }
    
    [ChildOf(typeof(PlayerComponent))]
    public sealed class Player : Entity, IAwake<string>
    {
        public string Account { get; set; }
        public PlayerState PlayerState;
        public long PlayerId;
    }
}