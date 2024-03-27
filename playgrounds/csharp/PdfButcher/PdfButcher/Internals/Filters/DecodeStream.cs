namespace PdfButcher.Internals.Filters
{
    using System;
    using System.IO;

    internal abstract class DecodeStream : Stream
    {
        protected readonly Stream inner;

        public DecodeStream(Stream inner)
        {
            this.inner = inner;
        }

        public override bool CanRead => inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException("DecodeStream can't read length");

        public override long Position
        {
            get => throw new NotSupportedException("DecodeStream can't get position");

            set => throw new NotSupportedException("DecodeStream can't set position");
        }

        public override void Flush()
        {
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
    }
}