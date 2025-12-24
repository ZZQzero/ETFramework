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
				_downloader = await ResourcesLoadManager.Instance.OnCreateDownLoad(package);
				OnCreateDownLoad();
			}
		}

		private async void OnCreateDownLoad()
		{
			if (_downloader.TotalDownloadCount == 0)
			{
				await ResourcesLoadManager.Instance.LoadHotfixDll();
				var handle = _package.LoadAssetSync<GameObject>("GameEntry");
				var obj = handle.InstantiateSync();
				GameObject.DontDestroyOnLoad(obj);
				Destroy(gameObject);
			}
			else
			{
				MessageBoxData messageBoxData = new MessageBoxData();
				float sizeMB = _downloader.TotalDownloadBytes / 1048576f;
				sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
				string totalSizeMB = sizeMB.ToString("f1");
				messageBoxData.Content = $"发现新资源需要更新，共{_downloader.TotalDownloadCount}个文件，总大小{totalSizeMB}MB";
				messageBoxData.OnClickOk = () =>
				{
					ResourcesLoadManager.Instance.BeginDownload(_downloader,OnDownLoadFinish,OnDownloadProgress,OnDownError);
					GameUIManager.Instance.CloseAndDestroyUI(LocalGameUIName.MessageBox);
				};
				await GameUIManager.Instance.OpenUI(LocalGameUIName.MessageBox, messageBoxData);
			}
		}

		private void OnDownError(DownloadErrorData data)
		{
			MessageBoxData messageBoxData = new MessageBoxData();
			messageBoxData.Content = $"下载失败，{data.FileName} , {data.ErrorInfo}";
			GameUIManager.Instance.OpenUI(LocalGameUIName.MessageBox, messageBoxData).Forget();
		}

		private void OnDownloadProgress(DownloadUpdateData data)
		{
			sliderSlider.value = (float)data.CurrentDownloadCount / data.TotalDownloadCount;
			string currentSizeMB = (data.CurrentDownloadBytes / 1048576f).ToString("f1");
			string totalSizeMB = (data.TotalDownloadBytes / 1048576f).ToString("f1");
			tipsText.text = $"{data.CurrentDownloadCount}/{data.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
		}

		private async void OnDownLoadFinish(DownloaderFinishData data)
		{
			if (data.Succeed)
			{
				await ResourcesLoadManager.Instance.LoadHotfixDll();
				var handle = _package.LoadAssetSync<GameObject>("GameEntry");
				var obj = handle.InstantiateSync();
				GameObject.DontDestroyOnLoad(obj);
				Destroy(gameObject);
			}
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
