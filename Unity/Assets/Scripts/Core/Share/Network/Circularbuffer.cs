using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    public class CircularBuffer : Stream
    {
        public int ChunkSize = 8192;

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
            _bufferCache.Enqueue(buf);

            if (_bufferQueue.Count > 0)
                FirstIndex = 0;
            else
                FirstIndex = LastIndex = 0;
        }

        public byte[] First
        {
            get
            {
                if (_bufferQueue.Count == 0) throw new InvalidOperationException("Buffer is empty");
                return _bufferQueue.Peek();
            }
        }

        public byte[] Last
        {
            get
            {
                if (_bufferQueue.Count == 0) throw new InvalidOperationException("Buffer is empty");
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
            while (_bufferQueue.Count > 0)
                _bufferCache.Enqueue(_bufferQueue.Dequeue());
            FirstIndex = LastIndex = 0;
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



/*using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    public class CircularBuffer: Stream
    {
        public int ChunkSize = 8192;

        private readonly Queue<byte[]> bufferQueue = new Queue<byte[]>();

        private readonly Queue<byte[]> bufferCache = new Queue<byte[]>();

        public int LastIndex { get; set; }

        public int FirstIndex { get; set; }
		
        private byte[] lastBuffer;

	    public CircularBuffer()
	    {
		    this.AddLast();
	    }

        public override long Length
        {
            get
            {
                int c = 0;
                if (this.bufferQueue.Count == 0)
                {
                    c = 0;
                }
                else
                {
                    c = (this.bufferQueue.Count - 1) * ChunkSize + this.LastIndex - this.FirstIndex;
                }
                if (c < 0)
                {
					Log.Error("CircularBuffer count < 0: {0}, {1}, {2}".Fmt(this.bufferQueue.Count, this.LastIndex, this.FirstIndex));
                }
                return c;
            }
        }

        public void AddLast()
        {
            byte[] buffer;
            if (this.bufferCache.Count > 0)
            {
                buffer = this.bufferCache.Dequeue();
            }
            else
            {
                buffer = new byte[ChunkSize];
            }
            this.bufferQueue.Enqueue(buffer);
            this.lastBuffer = buffer;
        }

        public void RemoveFirst()
        {
            this.bufferCache.Enqueue(bufferQueue.Dequeue());
        }

        public byte[] First
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
                    this.AddLast();
                }
                return this.bufferQueue.Peek();
            }
        }

        public byte[] Last
        {
            get
            {
                if (this.bufferQueue.Count == 0)
                {
                    this.AddLast();
                }
                return this.lastBuffer;
            }
        }

		/// <summary>
		/// 从CircularBuffer读到stream中
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		//public async ETTask ReadAsync(Stream stream)
	    //{
		//    long buffLength = this.Length;
		//	int sendSize = this.ChunkSize - this.FirstIndex;
		//    if (sendSize > buffLength)
		//    {
		//	    sendSize = (int)buffLength;
		//    }
		//	
		//    await stream.WriteAsync(this.First, this.FirstIndex, sendSize);
		//    
		//    this.FirstIndex += sendSize;
		//    if (this.FirstIndex == this.ChunkSize)
		//    {
		//	    this.FirstIndex = 0;
		//	    this.RemoveFirst();
		//    }
		//}

	    // 从CircularBuffer读到stream
	    public void Read(Stream stream, int count)
	    {
		    if (count > this.Length)
		    {
			    throw new Exception($"bufferList length < count, {Length} {count}");
		    }

		    int alreadyCopyCount = 0;
		    while (alreadyCopyCount < count)
		    {
			    int n = count - alreadyCopyCount;
			    if (ChunkSize - this.FirstIndex > n)
			    {
				    stream.Write(this.First, this.FirstIndex, n);
				    this.FirstIndex += n;
				    alreadyCopyCount += n;
			    }
			    else
			    {
				    stream.Write(this.First, this.FirstIndex, ChunkSize - this.FirstIndex);
				    alreadyCopyCount += ChunkSize - this.FirstIndex;
				    this.FirstIndex = 0;
				    this.RemoveFirst();
			    }
		    }
	    }
	    
	    // 从stream写入CircularBuffer
	    public void Write(Stream stream)
		{
			int count = (int)(stream.Length - stream.Position);
			
			int alreadyCopyCount = 0;
			while (alreadyCopyCount < count)
			{
				if (this.LastIndex == ChunkSize)
				{
					this.AddLast();
					this.LastIndex = 0;
				}

				int n = count - alreadyCopyCount;
				if (ChunkSize - this.LastIndex > n)
				{
					stream.Read(this.lastBuffer, this.LastIndex, n);
					this.LastIndex += count - alreadyCopyCount;
					alreadyCopyCount += n;
				}
				else
				{
					stream.Read(this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
					alreadyCopyCount += ChunkSize - this.LastIndex;
					this.LastIndex = ChunkSize;
				}
			}
		}
	    

	    /// <summary>
		///  从stream写入CircularBuffer
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		//public async ETTask<int> WriteAsync(Stream stream)
	    //{
		//    int size = this.ChunkSize - this.LastIndex;
		//    
		//    int n = await stream.ReadAsync(this.Last, this.LastIndex, size);
//
		//    if (n == 0)
		//    {
		//	    return 0;
		//    }
//
		//    this.LastIndex += n;
//
		//    if (this.LastIndex == this.ChunkSize)
		//    {
		//	    this.AddLast();
		//	    this.LastIndex = 0;
		//    }
//
		//    return n;
	    //}

	    // 把CircularBuffer中数据写入buffer
        public override int Read(byte[] buffer, int offset, int count)
        {
	        if (buffer.Length < offset + count)
	        {
		        throw new Exception($"bufferList length < coutn, buffer length: {buffer.Length} {offset} {count}");
	        }

	        long length = this.Length;
			if (length < count)
            {
	            count = (int)length;
            }

            int alreadyCopyCount = 0;
            while (alreadyCopyCount < count)
            {
                int n = count - alreadyCopyCount;
				if (ChunkSize - this.FirstIndex > n)
                {
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, n);
                    this.FirstIndex += n;
                    alreadyCopyCount += n;
                }
                else
                {
                    Array.Copy(this.First, this.FirstIndex, buffer, alreadyCopyCount + offset, ChunkSize - this.FirstIndex);
                    alreadyCopyCount += ChunkSize - this.FirstIndex;
                    this.FirstIndex = 0;
                    this.RemoveFirst();
                }
            }

	        return count;
        }

	    // 把buffer写入CircularBuffer中
        public override void Write(byte[] buffer, int offset, int count)
        {
	        int alreadyCopyCount = 0;
            while (alreadyCopyCount < count)
            {
                if (this.LastIndex == ChunkSize)
                {
                    this.AddLast();
                    this.LastIndex = 0;
                }

                int n = count - alreadyCopyCount;
                if (ChunkSize - this.LastIndex > n)
                {
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, n);
                    this.LastIndex += count - alreadyCopyCount;
                    alreadyCopyCount += n;
                }
                else
                {
                    Array.Copy(buffer, alreadyCopyCount + offset, this.lastBuffer, this.LastIndex, ChunkSize - this.LastIndex);
                    alreadyCopyCount += ChunkSize - this.LastIndex;
                    this.LastIndex = ChunkSize;
                }
            }
        }

	    public override void Flush()
	    {
		    throw new NotImplementedException();
		}

	    public override long Seek(long offset, SeekOrigin origin)
	    {
			throw new NotImplementedException();
	    }

	    public override void SetLength(long value)
	    {
		    throw new NotImplementedException();
		}

	    public override bool CanRead
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override bool CanSeek
	    {
		    get
		    {
			    return false;
		    }
	    }

	    public override bool CanWrite
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override long Position { get; set; }
    }
}*/