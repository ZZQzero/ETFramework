using ET;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class UILoginPanel : GameUIBase
	{
		public override void OnInitUI()
		{
			base.OnInitUI();
			loginBtnButton.onClick.AddListener(OnLoginClick);
		}

		private void OnLoginClick()
		{
			LoginHelper.Login(
				root, 
				globalComponent.GlobalConfig.IPAddress,
				accountInputField.text, 
				passwordInputField.text).NoContext();
		}

		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data is Scene scene)
			{
				root = scene;
				globalComponent = scene.GetComponent<GlobalComponent>();
			}
		}
		public override void OnRefreshUI()
		{
			base.OnRefreshUI();
		}
		public override void OnCloseUI()
		{
			base.OnCloseUI();
		}
		public override void OnDestroyUI()
		{
			base.OnDestroyUI();
		}
	}
}
