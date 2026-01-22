namespace ET
{
    /// <summary>
    /// Unity场景加载完成的等待类型
    /// </summary>
    public struct Wait_UnitySceneLoaded: IWaitType
    {
        public int Error
        {
            get;
            set;
        }
    }
}