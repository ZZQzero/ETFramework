using Nino.Core;

namespace ET
{
    [Message(ushort.MaxValue)]
    [NinoType(false)]
    public partial class MessageResponse: MessageObject, IResponse
    {
        [NinoMember(1)]
        public int RpcId { get; set; }
        [NinoMember(2)]
        public int Error { get; set; }
        [NinoMember(3)]
        public string Message { get; set; }
    }
}