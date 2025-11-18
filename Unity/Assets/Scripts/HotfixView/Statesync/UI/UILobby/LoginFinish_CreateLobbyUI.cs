using GameUI;

namespace ET
{
	[Event(SceneType.Main)]
	public class LoginFinish_CreateLobbyUI: AEvent<Scene, LoginFinish>
	{
		protected override async ETTask Run(Scene scene, LoginFinish args)
		{
			await GameUIManager.Instance.OpenUI(GameUIName.UILobby, scene);
			//await UIHelper.Create(scene, UIType.UILobby, UILayer.Mid);
		}
	}
}
