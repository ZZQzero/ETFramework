using GameUI;
using UnityEngine;

namespace ET
{
	public class AppStartInitFinish_CreateLoginUI: AEvent<Scene, AppStartInitFinish>
	{
		protected override async ETTask Run(Scene root, AppStartInitFinish args)
		{
			Debug.LogError("AppStartInitFinish_CreateLoginUI");
			//await UIHelper.Create(root, UIType.UILogin, UILayer.Mid);
			await GameUIManager.Instance.OpenUI(GameUIName.UILogin, root);
		}
	}
}
