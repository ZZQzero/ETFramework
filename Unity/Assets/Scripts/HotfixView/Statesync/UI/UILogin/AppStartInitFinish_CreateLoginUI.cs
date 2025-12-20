using GameUI;
using UnityEngine;

namespace ET
{
	[Event(SceneType.Main)]
	public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
	{
		protected override async ETTask Run(Scene root, AppStartInitFinish args)
		{
			Debug.LogError("AppStartInitFinish_CreateLoginUI");
			//await UIHelper.Create(root, UIType.UILogin, UILayer.Mid);
            EventSystem.Instance.PublishAsync(root, new SceneChangeStart(){SceneName = UnityScene.Login}).NoContext();
            GameUIManager.Instance.CloseAndDestroyUI(GameUIName.UIHelp);
			await GameUIManager.Instance.OpenUI(GameUIName.UILogin, root);
		}
	}
}
