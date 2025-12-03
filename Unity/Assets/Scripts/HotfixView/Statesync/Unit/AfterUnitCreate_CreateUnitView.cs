using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Debug.LogError("AfterUnitCreate_CreateUnitView");
            Unit unit = args.Unit;
            // Unit View层
            string assetsName = $"Skeleton";
            GameObject go = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(assetsName);
            go.transform.position = unit.Position;
            go.transform.localScale = Vector3.one * 10;
            UnityEngine.Object.DontDestroyOnLoad(go);
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<AnimatorComponent>();
            await ETTask.CompletedTask;
        }
    }
}