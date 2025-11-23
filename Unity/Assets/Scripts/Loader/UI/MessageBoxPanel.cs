using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameUI
{
	public class MessageBoxData
	{
		public string Content;
		public Action OnClickOk;
	}
	public partial class MessageBoxPanel : GameUIBase
	{
		public override void OnInitUI()
		{
			base.OnInitUI();
			okButton.onClick.AddListener(OnOnClick);
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
			if (Data is MessageBoxData data)
			{
				messageData = data;
				contentText.text = data.Content;
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
		
		private void OnOnClick()
		{
			messageData.OnClickOk?.Invoke();
		}
	}
}
