namespace ET
{
    public static partial class UnitHelper
    {
        public static Unit GetMyUnitFromClientScene(Scene root)
        {
            UserComponent userComponent = root.GetComponent<UserComponent>();
            Scene currentScene = root.GetComponent<CurrentScenesComponent>().Scene;
            return currentScene.GetComponent<UnitComponent>().Get(userComponent.CurrentRoleId);
        }
        
        public static Unit GetMyUnitFromCurrentScene(Scene currentScene)
        {
            UserComponent userComponent = currentScene.Root().GetComponent<UserComponent>();
            return currentScene.GetComponent<UnitComponent>().Get(userComponent.CurrentRoleId);
        }
    }
}