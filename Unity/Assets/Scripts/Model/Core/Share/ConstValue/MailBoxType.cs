namespace ET
{
    public static partial class MailBoxType
    {
        public const int UnOrderedMessage = PackageType.Core * ConstValue.Seed + 1;
        public const int OrderedMessage = PackageType.ActorLocation * ConstValue.Seed + 1;
        public const int GateSession = PackageType.Login * ConstValue.Seed + 1;
    }
}