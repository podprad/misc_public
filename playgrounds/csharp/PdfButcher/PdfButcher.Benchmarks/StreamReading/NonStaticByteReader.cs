namespace PdfButcher.Benchmarks.StreamReading
{
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class NonStaticByteReader
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer;

        private int _bufferPosition = -1;
        private int _bufferFill = -1;

        public NonStaticByteReader(Stream stream, int bufferLength)
        {
            _stream = stream;
            _buffer = new byte[bufferLength];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadByte()
        {
            if (_bufferPosition > _bufferFill - 1 || _bufferPosition < 0)
            {
                _bufferFill = _stream.Read(_buffer, 0, _buffer.Length);
                _bufferPosition = 0;

                if (_bufferFill == 0)
                {
                    return -1;
                }
            }

            return _buffer[_bufferPosition++];
        }
    }
}