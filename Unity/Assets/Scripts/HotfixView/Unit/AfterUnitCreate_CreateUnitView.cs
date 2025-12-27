using UnityEngine;

namespace ET
{
    [Event(SceneType.Main)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            // Unit View层
            string assetsName = $"Ellen";
            GameObject go = await ResourcesLoadManager.Instance.LoadGameObjectAsync(assetsName);
            go.transform.position = unit.Position;
            go.transform.localScale = Vector3.one;
            UnityEngine.Object.DontDestroyOnLoad(go);
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            unit.AddComponent<InputComponent>();
            unit.AddComponent<CheckGroundedComponent,GameObject>(go);
            unit.AddComponent<CharacterControllerComponent,GameObject>(go);
            unit.AddComponent<AnimatorComponent>();
            await ETTask.CompletedTask;
        }
    }
}