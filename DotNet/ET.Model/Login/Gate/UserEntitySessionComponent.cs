namespace ET
{
	[ComponentOf(typeof(UserEntity))]
	public class UserEntitySessionComponent : Entity, IAwake
	{
		private EntityRef<Session> session;

		public Session Session
		{
			get => this.session;
			set => this.session = value;
		}
	}
}