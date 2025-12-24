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
                Scene currentScene = root;
                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();
                // 加载场景资源
                //await resourcesLoaderComponent.LoadSceneAsync(args.SceneName, LoadSceneMode.Single);
                ResourcesLoadManager.Instance.LoadSceneAsync(args.SceneName,LoadSceneMode.Single).NoContext();
                // 切换到map场景

                //await SceneManager.LoadSceneAsync(currentScene.Name);

                currentScene.RemoveComponent<OperaComponent>();
                currentScene.AddComponent<OperaComponent,Scene>(currentScene);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}