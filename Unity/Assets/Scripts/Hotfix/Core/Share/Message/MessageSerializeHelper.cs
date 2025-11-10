using System;
using System.Buffers.Binary;
using System.IO;
using Nino.Core;

namespace ET
{
    public static class MessageSerializeHelper
    {
        public static ushort MessageToStream(MemoryBuffer stream, MessageObject message, int headOffset = 0)
        {
            ushort opcode = MessageOpcodeTypeMap.TypeToOpcode[message.GetType()];
            
            stream.Seek(headOffset + Packet.OpcodeLength, SeekOrigin.Begin);
            stream.SetLength(headOffset + Packet.OpcodeLength);
            
            stream.GetBuffer().WriteTo(headOffset, opcode);
            NinoSerializer.Serialize(message, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return opcode;
        }
        
        public static (ushort, MemoryBuffer) ToMemoryBuffer(AService service, ActorId actorId, object message)
        {
            MemoryBuffer memoryBuffer = service.Fetch();
            ushort opcode = 0;
            switch (service.ServiceType)
            {
                case ServiceType.Inner:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message, Packet.ActorIdLength);
                    memoryBuffer.GetBuffer().WriteTo(0, actorId);
                    break;
                }
                case ServiceType.Outer:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message);
                    break;
                }
            }
            
            return (opcode, memoryBuffer);
        }
        
        public static (ActorId, object) ToMessage(AService service, MemoryBuffer memoryStream)
        {
            MessageObject message = null;
            var buf = memoryStream.GetBuffer();
            ActorId actorId = default;
            switch (service.ServiceType)
            {
                case ServiceType.Outer:
                {
                    ushort opcode = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(0, Packet.OpcodeLength));
                    memoryStream.Seek(Packet.OpcodeLength, SeekOrigin.Begin);
                    message = MessageOpcodeTypeMap.OpcodeToMessage[opcode](memoryStream);
                    break;
                }
                case ServiceType.Inner:
                {
                    actorId.Process = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(0, 4));
                    actorId.Fiber = BinaryPrimitives.ReadInt32LittleEndian(buf.AsSpan(4, 4));
                    actorId.InstanceId = BinaryPrimitives.ReadInt64LittleEndian(buf.AsSpan(8, 8));
                    ushort opcode = BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(Packet.ActorIdLength, Packet.OpcodeLength));
                    memoryStream.Seek(Packet.ActorIdLength + Packet.OpcodeLength, SeekOrigin.Begin);
                    message = MessageOpcodeTypeMap.OpcodeToMessage[opcode](memoryStream);
                    break;
                }
            }
            
            return (actorId, message);
        }
    }
}