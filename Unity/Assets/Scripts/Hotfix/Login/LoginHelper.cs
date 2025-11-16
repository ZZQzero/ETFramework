namespace ET
{
    public static class LoginHelper
    {
        public static async ETTask Login(Scene root, string account, string password)
        {
            root.RemoveComponent<ClientSenderComponent>();
            ClientSenderComponent clientSenderComponent = root.AddComponent<ClientSenderComponent>();
            var resp = await clientSenderComponent.LoginAsync(GlobalConfigManager.Instance.Config.IPAddress, account, password);
            if (resp.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"login failed {resp.Error}");
                return;
            }
            root.GetComponent<PlayerComponent>().MyId = resp.PlayerId;
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
        }
    }
}