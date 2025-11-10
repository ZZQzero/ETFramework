using GameUI;

namespace ET
{
	public class LoginFinish_RemoveLoginUI: AEvent<Scene, LoginFinish>
	{
		protected override async ETTask Run(Scene scene, LoginFinish args)
		{
			GameUIManager.Instance.CloseAndDestroyUI(GameUIName.UILogin);
			//await UIHelper.Remove(scene, UIType.UILogin);
		}
	}
}
