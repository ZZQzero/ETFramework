using GameUI;
using UnityEngine;

namespace ET.Client
{
    [UIEvent(UIType.UILobby)]
    public class UILobbyEvent: AUIEvent
    {
        public override async ETTask<UI> OnCreate(UIComponent uiComponent, UILayer uiLayer)
        {
            await ETTask.CompletedTask;
            string assetsName = $"{UIType.UILobby}";
            GameObject bundleGameObject = await uiComponent.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);
            bundleGameObject.transform.SetParent(GameUIManager.Instance.GetUILayer(EGameUILayer.Normal));
            UI ui = uiComponent.AddChild<UI, string, GameObject>(UIType.UILobby, bundleGameObject);
            bundleGameObject.transform.localPosition = Vector3.zero;
            bundleGameObject.transform.localScale = Vector3.one;
            ui.AddComponent<UILobbyComponent>();
            return ui;
        }

        public override void OnRemove(UIComponent uiComponent)
        {
        }
    }
}