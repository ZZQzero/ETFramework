namespace ET
{
	[ComponentOf(typeof(Player))]
	public class PlayerSessionComponent : Entity, IAwake
	{
		private EntityRef<Session> session;

		public Session Session
		{
			get => this.session;
			set => this.session = value;
		}
	}
}