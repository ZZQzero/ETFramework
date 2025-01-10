using System;
using GameUI;
using UnityEngine;

namespace ET.Client
{
	[UIEvent(UIType.UIHelp)]
    public class UIHelpEvent: AUIEvent
    {
        public override async ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer)
        {
	        try
	        {
		        string assetsName = $"{UIType.UIHelp}";
		        GameObject bundleGameObject = await uiComponent.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);
		        bundleGameObject.transform.SetParent(GameUIManager.Instance.GetUILayer(EGameUILayer.Normal));
		        UI ui = uiComponent.AddChild<UI, string, GameObject>(UIType.UIHelp, bundleGameObject);
		        bundleGameObject.transform.localPosition = Vector3.zero;
		        bundleGameObject.transform.localScale = Vector3.one;
				ui.AddComponent<UIHelpComponent>();
				return ui;
	        }
	        catch (Exception e)
	        {
		        Log.Error(e);
		        return null;
	        }
		}

        public override void OnRemove(UIComponent uiComponent)
        {
        }
    }
}