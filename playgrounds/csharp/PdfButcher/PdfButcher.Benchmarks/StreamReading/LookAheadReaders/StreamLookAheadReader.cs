namespace PdfButcher.Benchmarks.StreamReading.LookAheadReaders
{
    using System.IO;
    using PdfButcher.Internals.IO.LookAhead;

    public class StreamLookAheadReader : LookAheadReaderBase<int>
    {
        private readonly Stream _stream;

        public StreamLookAheadReader(Stream stream)
        {
            _stream = stream;
        }

        protected override int ReadNextCore()
        {
            return _stream.ReadByte();
        }
    }
}