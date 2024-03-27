namespace PdfButcher.Benchmarks.StreamReading.LookAheadReaders
{
    using System.IO;
    using PdfButcher.Internals.IO.LookAhead;

    public class BufferedStreamLookAheadReader : LookAheadReaderBase<int>
    {
        private readonly BufferedStream _bufferedStream;

        public BufferedStreamLookAheadReader(Stream stream, int bufferSize)
        {
            _bufferedStream = new BufferedStream(stream, bufferSize);
        }

        protected override int ReadNextCore()
        {
            return _bufferedStream.ReadByte();
        }
    }
}