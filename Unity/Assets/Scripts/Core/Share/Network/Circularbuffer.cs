using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    public class CircularBuffer : Stream
    {
        public int ChunkSize = 8192;
        
        private const int MaxCacheBlocks = 32; // 限制缓存块数，避免无限膨胀
        private readonly Queue<byte[]> _bufferQueue = new Queue<byte[]>();
        private readonly Queue<byte[]> _bufferCache = new Queue<byte[]>();
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

        private byte[] _lastBuffer;

        public int FirstIndex;
        public int LastIndex;

        public CircularBuffer()
        {
            AddLast();
        }

        public override long Length => _bufferQueue.Count == 0 ? 0 : (_bufferQueue.Count - 1L) * ChunkSize + LastIndex - FirstIndex;

        public void AddLast()
        {
            byte[] buffer = _bufferCache.Count > 0 ? _bufferCache.Dequeue() : _pool.Rent(ChunkSize);
            _bufferQueue.Enqueue(buffer);
            _lastBuffer = buffer;
            if (_bufferQueue.Count == 1)
            {
                FirstIndex = 0;
                LastIndex = 0;
            }
        }

        public void RemoveFirst()
        {
            if (_bufferQueue.Count == 0) return;

            var buf = _bufferQueue.Dequeue();
            if (_bufferCache.Count < MaxCacheBlocks)
            {
                _bufferCache.Enqueue(buf);
            }
            else
            {
                _pool.Return(buf, clearArray: false);
            }

            if (_bufferQueue.Count > 0)
            {
                FirstIndex = 0;
            }
            else
            {
                FirstIndex = LastIndex = 0;
            }
        }

        public byte[] First
        {
            get
            {
                // 确保始终可用一个块，避免上层在空队列时取 First 导致异常
                if (_bufferQueue.Count == 0)
                {
                    AddLast();
                    FirstIndex = 0;
                    LastIndex = 0;
                }
                return _bufferQueue.Peek();
            }
        }

        public byte[] Last
        {
            get
            {
                // 与 First 一致：空时自动扩容，便于网络接收直接填充
                if (_bufferQueue.Count == 0)
                {
                    AddLast();
                    FirstIndex = 0;
                    LastIndex = 0;
                }
                return _lastBuffer;
            }
        }

        // 高性能读 Stream
        public void Read(Stream stream, int count)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (count > Length) throw new ArgumentException($"Requested {count}, only {Length} available");

            int remaining = count;
            while (remaining > 0)
            {
                byte[] current = First;
                int bytesToCopy = Math.Min(ChunkSize - FirstIndex, remaining);

                // 一次性写入 Stream，避免小块多次调用
                stream.Write(current, FirstIndex, bytesToCopy);

                FirstIndex += bytesToCopy;
                remaining -= bytesToCopy;

                if (FirstIndex == ChunkSize)
                    RemoveFirst();
            }
        }

        // 高性能写 Stream
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            long bytesAvailable = stream.Length - stream.Position;
            if (bytesAvailable <= 0) return;

            int remaining = (int)bytesAvailable;
            while (remaining > 0)
            {
                if (LastIndex == ChunkSize)
                {
                    AddLast();
                    LastIndex = 0;
                }

                byte[] current = Last;
                int bytesToCopy = Math.Min(ChunkSize - LastIndex, remaining);

                int read = stream.Read(current, LastIndex, bytesToCopy);
                if (read <= 0) break;

                LastIndex += read;
                remaining -= read;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset/count");

            int bytesToRead = (int)Math.Min(Length, count);
            if (bytesToRead == 0) return 0;

            int remaining = bytesToRead;
            int targetOffset = offset;

            while (remaining > 0)
            {
                byte[] current = First;
                int bytesFromCurrent = Math.Min(ChunkSize - FirstIndex, remaining);

                // 使用 Span<byte> 高效拷贝
                current.AsSpan(FirstIndex, bytesFromCurrent).CopyTo(buffer.AsSpan(targetOffset, bytesFromCurrent));

                FirstIndex += bytesFromCurrent;
                targetOffset += bytesFromCurrent;
                remaining -= bytesFromCurrent;

                if (FirstIndex == ChunkSize)
                    RemoveFirst();
            }

            return bytesToRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || buffer.Length - offset < count)
                throw new ArgumentException("Invalid offset/count");

            if (count == 0) return;

            int remaining = count;
            int sourceOffset = offset;

            while (remaining > 0)
            {
                if (LastIndex == ChunkSize)
                {
                    AddLast();
                    LastIndex = 0;
                }

                byte[] current = Last;
                int bytesToCopy = Math.Min(ChunkSize - LastIndex, remaining);

                buffer.AsSpan(sourceOffset, bytesToCopy).CopyTo(current.AsSpan(LastIndex, bytesToCopy));

                LastIndex += bytesToCopy;
                sourceOffset += bytesToCopy;
                remaining -= bytesToCopy;
            }
        }

        public void Clear()
        {
            // 将现有队列里的块释放回池或缓存
            while (_bufferQueue.Count > 0)
            {
                var buf = _bufferQueue.Dequeue();
                if (_bufferCache.Count < MaxCacheBlocks)
                {
                    _bufferCache.Enqueue(buf);
                }
                else
                {
                    _pool.Return(buf, clearArray: false);
                }
            }

            // 重置索引，并确保至少存在一个可写入块，避免上层立刻访问 Last/First 时异常
            FirstIndex = 0;
            LastIndex = 0;
            _lastBuffer = null;
            AddLast();
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (_bufferQueue.Count > 0) _pool.Return(_bufferQueue.Dequeue(), clearArray: false);
                while (_bufferCache.Count > 0) _pool.Return(_bufferCache.Dequeue(), clearArray: false);
                _lastBuffer = null;
            }

            base.Dispose(disposing);
        }
    }
}
