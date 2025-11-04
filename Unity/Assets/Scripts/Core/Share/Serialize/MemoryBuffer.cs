using System;
using System.Buffers;
using System.IO;
using Nino.Core;

namespace ET
{
    public class MemoryBuffer: MemoryStream, INinoBufferWriter
    {
        private int origin;
        
        public MemoryBuffer()
        {
        }
        
        public MemoryBuffer(int capacity): base(capacity)
        {
        }
        
        public MemoryBuffer(byte[] buffer): base(buffer)
        {
        } 
        
        public MemoryBuffer(byte[] buffer, int index, int length): base(buffer, index, length)
        {
            this.origin = index;
        }
        
        public ReadOnlyMemory<byte> WrittenMemory => this.GetBuffer().AsMemory(this.origin, (int)this.Position);

        public ReadOnlySpan<byte> WrittenSpan => this.GetBuffer().AsSpan(this.origin, (int)this.Position);

        public int WrittenCount => (int)this.Position;

        public void Advance(int count)
        {
            long newLength = this.Position + count;
            if (newLength > this.Length)
            {
                this.SetLength(newLength);
            }
            this.Position = newLength;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint <= 0)
            {
                sizeHint = 1; // IBufferWriter 允许 0，但返回 0 长度对上层不友好
            }
            long required = this.Position + sizeHint;
            if (required > this.Length)
            {
                this.SetLength(required);
            }
            var memory = this.GetBuffer().AsMemory((int)this.Position + this.origin, (int)(this.Length - this.Position));
            return memory;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint <= 0)
            {
                sizeHint = 1; // 确保返回非空 Span，便于上层直接写入
            }
            long required = this.Position + sizeHint;
            if (required > this.Length)
            {
                this.SetLength(required);
            }
            var span = this.GetBuffer().AsSpan((int)this.Position + this.origin, (int)(this.Length - this.Position));
            return span;
        }
    }
}