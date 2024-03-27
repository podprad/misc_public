namespace PdfButcher.Benchmarks.StreamReading.LookAheadReaders
{
    using System.IO;
    using PdfButcher.Internals.IO.LookAhead;

    public class CustomBufferingLookAheadReader : LookAheadReaderBase<int>
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private int _bufferPosition;
        private int _bufferFill;

        public CustomBufferingLookAheadReader(Stream stream, int bufferSize = 32 * 1024)
        {
            _stream = stream;

            _buffer = new byte[bufferSize];

            StaticByteReader.InitRead(out _bufferPosition, out _bufferFill, out _buffer, _buffer.Length);
        }

        protected override int ReadNextCore()
        {
            return StaticByteReader.ReadByte(_stream, ref _bufferPosition, ref _bufferFill, _buffer);
        }
    }
}