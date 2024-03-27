namespace PdfButcher.Benchmarks.StreamReading.LookAheadReaders
{
    using System.IO;
    using PdfButcher.Internals.IO.LookAhead;

    public class RawStreamLookAheadReader : LookAheadReaderBase<int>
    {
        private readonly Stream _bufferedStream;

        public RawStreamLookAheadReader(Stream stream)
        {
            _bufferedStream = stream;
        }

        protected override int ReadNextCore()
        {
            return _bufferedStream.ReadByte();
        }
    }
}