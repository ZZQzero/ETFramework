using System;
using Cysharp.Threading.Tasks;
using ET;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace GameUI
{
	public partial class PatchPanelPanel : GameUIBase
	{
		private ResourceDownloaderOperation _downloader;
		private ResourcePackage _package;
		public override void OnInitUI()
		{
			base.OnInitUI();
			#region Auto Generate Code
			InitData();
			#endregion Auto Generate Code
			
		}
		public override void OnOpenUI()
		{
			base.OnOpenUI();
		}
		
		public override async void OnRefreshUI()
		{
			base.OnRefreshUI();
			if(Data != null && Data is ResourcePackage package)
			{
				_package = package;
				_downloader = await ResourcesComponent.Instance.OnCreateDownLoad(package);
				Debug.LogError($"OnOpenUI  {_downloader}  {_downloader.TotalDownloadCount}");
				OnCreateDownLoad();
			}
		}

		private void OnCreateDownLoad()
		{
			if (_downloader.TotalDownloadCount == 0)
			{
				var handle = _package.LoadAssetSync<GameObject>("GameEntry");
				var obj = handle.InstantiateSync();
				GameObject.DontDestroyOnLoad(obj);
				Debug.LogError("不用下载");
				Destroy(gameObject);
			}
			else
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				float sizeMB = _downloader.TotalDownloadBytes / 1048576f;
				sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
				string totalSizeMB = sizeMB.ToString("f1");
				messageBoxData.Content = $"发现新资源需要更新，共{_downloader.TotalDownloadCount}个文件，总大小{totalSizeMB}MB";
				messageBoxData.OnOk = () =>
				{
					ResourcesComponent.Instance.BeginDownload(_downloader,OnDownLoadFinish,OnDownloadProgress,OnDownError);
				};
				GameUIManager.Instance.OpenUI(GameUIName1.MessageBoxPanel, messageBoxData);
			}
		}

		private void OnDownError(DownloadErrorData data)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.Content = $"下载失败，{data.FileName} , {data.ErrorInfo}";
			GameUIManager.Instance.OpenUI(GameUIName1.MessageBoxPanel, messageBoxData).Forget();
		}

		private void OnDownloadProgress(DownloadUpdateData data)
		{
			SliderSlider.value = (float)data.CurrentDownloadCount / data.TotalDownloadCount;
			string currentSizeMB = (data.CurrentDownloadBytes / 1048576f).ToString("f1");
			string totalSizeMB = (data.TotalDownloadBytes / 1048576f).ToString("f1");
			TxttipsText.text = $"{data.CurrentDownloadCount}/{data.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
		}

		private void OnDownLoadFinish(DownloaderFinishData data)
		{
			if (data.Succeed)
			{
				Debug.LogError("下载完成");
				Destroy(gameObject);
				//TODO 正式进入游戏
			}
		}

		private void ChangeScene()
		{
			YooAssets.LoadSceneAsync("scene_home");
			GameUIManager.Instance.OpenUI(GameUIName1.UIHome,null).Forget();
			CloseSelf();
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
