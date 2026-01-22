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
                Log.Info($"开始加载Unity场景: {args.SceneName}");
                
                // 异步加载Unity场景
                await ResourcesLoadManager.Instance.LoadSceneAsync(
                    args.SceneName, 
                    LoadSceneMode.Single,
                    call: progress =>
                    {
                        // 更新Loading进度条（可选）
                        Log.Info($"场景加载进度: {progress * 100:F1}%");
                    },
                    onComplete: OnSceneLoaded);

                void OnSceneLoaded()
                {
                    Log.Info($"Unity场景资源加载完成: {args.SceneName}");
                    
                    // TODO: 场景加载完成后的初始化工作
                    // 例如：初始化导航网格、地形、光照等
                    
                    // 通知SceneChangeTo协程：Unity场景已加载完成
                    root.GetComponent<ObjectWait>().Notify(new Wait_UnitySceneLoaded() { Error = ErrorCode.ERR_Success });
                }
            }
            catch (Exception e)
            {
                Log.Error($"场景加载失败: {args.SceneName}, Exception: {e}");
                
                // 通知SceneChangeTo协程：场景加载失败
                root.GetComponent<ObjectWait>().Notify(new Wait_UnitySceneLoaded() { Error = ErrorCode.ERR_SceneLoadError });
            }
        }
    }
}