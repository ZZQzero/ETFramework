using ET;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public partial class UILobbyPanel : GameUIBase
	{
		private Scene root;
		public override void OnInitUI()
		{
			base.OnInitUI();
			enterMapButton.onClick.AddListener(async delegate
			{
				await EnterMapHelper.EnterMapAsync(root);
				GameUIManager.Instance.CloseAndDestroyUI(GameUIName.UILobby);
			});
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data is Scene scene)
			{
				root = scene;
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
