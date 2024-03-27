namespace PdfButcher.Internals.StreamExtensions.Internals
{
    using System;
    using System.IO;

    internal class StreamBytesSource : IBytesSource
    {
        private readonly Stream _stream;

        public StreamBytesSource(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }
    }
}