using GameUI;

namespace ET
{
    [Event(SceneType.Current)]
    public class SceneChangeFinishEvent_CreateUIHelp : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            //await UIHelper.Create(scene, UIType.UIHelp, UILayer.Mid);
            await GameUIManager.Instance.OpenUI(GameUIName.UIHelp, null);
        }
    }
}
