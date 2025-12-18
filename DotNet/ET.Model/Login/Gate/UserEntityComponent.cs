using System.Collections.Generic;
using System.Linq;

namespace ET
{
	[ComponentOf(typeof(Scene))]
	public class UserEntityComponent : Entity, IAwake, IDestroy
	{
		public Dictionary<long, EntityRef<UserEntity>> dictionary = new Dictionary<long, EntityRef<UserEntity>>();
	}
}