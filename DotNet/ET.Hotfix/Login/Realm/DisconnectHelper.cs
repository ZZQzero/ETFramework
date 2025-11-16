namespace ET;

public static class DisconnectHelper
{
    public static async ETTask Disconnect(this Session self)
    {
        if (self == null || self.IsDisposed)
        {
            return;
        }
        long instanceId = self.InstanceId;
        
        TimerComponent timerComponent = self.Root().GetComponent<TimerComponent>();
        await timerComponent.WaitAsync(1000);

        if (self.InstanceId != instanceId)
        {
            return;
        }
        self.Dispose();
    }
}