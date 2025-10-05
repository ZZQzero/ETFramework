namespace ET
{
	[ComponentOf(typeof(Session))]
	public class SessionPlayerComponent : Entity, IAwake, IDestroy
	{
		private EntityRef<Player> player;

		public Player Player
		{
			get => this.player;
			set => this.player = value;
		}
	}
}