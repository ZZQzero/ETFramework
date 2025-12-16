namespace ET
{
	[ComponentOf(typeof(Session))]
	public class SessionUserEntityComponent : Entity, IAwake, IDestroy
	{
		private EntityRef<UserEntity> _userEntity;

		public UserEntity UserEntity
		{
			get => this._userEntity;
			set => this._userEntity = value;
		}
	}
}