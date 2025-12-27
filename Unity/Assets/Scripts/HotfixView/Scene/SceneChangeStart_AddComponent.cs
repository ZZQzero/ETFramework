using System;
using UnityEngine.SceneManagement;

namespace ET
{
    [Event(SceneType.Main)]
    public class SceneChangeStart_AddComponent: AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                ResourcesLoadManager.Instance.LoadSceneAsync(args.SceneName,LoadSceneMode.Single,onComplete:OnChangeSceneEnd).NoContext();

                void OnChangeSceneEnd()
                {
                    if (args.SceneName == UnityScene.Map1)
                    {
                        EventSystem.Instance.Publish(root, new SceneChangeFinish());
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}