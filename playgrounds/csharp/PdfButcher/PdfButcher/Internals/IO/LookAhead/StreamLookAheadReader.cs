namespace PdfButcher.Internals.IO.LookAhead
{
    using System.IO;

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