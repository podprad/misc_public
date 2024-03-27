namespace PdfButcher.Internals.Filters
{
    using System;
    using System.IO;

    internal abstract class MinBufferStream : Stream
    {
        protected readonly Stream inner;
        private readonly long position = 0;

        private byte[] buffer;

        private int read = 0;
        private int leftToRead = 0;
        private int minimum = 0;

        public MinBufferStream(Stream inner, int minimum)
        {
            this.inner = inner;
            this.minimum = minimum;
            this.buffer = new byte[minimum];
        }

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException("DecodeStream can't read length");

        public override long Position
        {
            get => position;

            set => throw new NotSupportedException("DecodeStream can't set position");
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] outgoing, int offset, int count)
        {
            if (leftToRead > 0)
            {
                if (leftToRead > count)
                {
                    Array.Copy(buffer, read - leftToRead, outgoing, offset, count);
                    leftToRead -= count;

                    return count;
                }
                else
                {
                    Array.Copy(buffer, read - leftToRead, outgoing, offset, leftToRead);
                    var rd = leftToRead;
                    leftToRead = 0;

                    return rd;
                }
            }

            if (count >= minimum)
            {
                return FillBuffer(outgoing, offset, count);
            }

            read = FillBuffer(buffer, 0, minimum);
            if (read <= 0)
            {
                return 0;
            }

            Array.Copy(buffer, 0, outgoing, offset, count);
            leftToRead = read - count;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("DecodeStream can't set position");
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("DecodeStream can't set length");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("DecodeStream can't set position");
        }

        public new void Dispose()
        {
            inner.Dispose();
            base.Dispose();
        }

        protected abstract int FillBuffer(byte[] outgoing, int offset, int count);
    }
}