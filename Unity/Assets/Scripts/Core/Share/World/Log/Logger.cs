namespace ET
{
    public class Logger: Singleton<Logger>, ISingletonAwake
    {
        public ILog Log { set; get; }

        public void Awake()
        {
        }
    }
}