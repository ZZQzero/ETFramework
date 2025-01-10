using System;
using GameUI;
using UnityEngine;

namespace ET.Client
{
    [UIEvent(UIType.UILogin)]
    public class UILoginEvent: AUIEvent
    {
        public override async ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer)
        {
            string assetsName = $"{UIType.UILogin}";
            var load = uiComponent.Scene().GetComponent<ResourcesLoaderComponent>();
            GameObject bundleGameObject = await load.LoadAssetAsync<GameObject>(assetsName);
            Debug.LogError($"{bundleGameObject}");
            bundleGameObject.transform.SetParent(GameUIManager.Instance.GetUILayer(EGameUILayer.Normal));
            bundleGameObject.transform.localPosition = Vector3.zero;
            bundleGameObject.transform.localScale = Vector3.one;
            UI ui = uiComponent.AddChild<UI, string, GameObject>(UIType.UILogin, bundleGameObject);
            ui.AddComponent<UILoginComponent>();
            return ui;
        }

        public override void OnRemove(UIComponent uiComponent)
        {
        }
    }
}