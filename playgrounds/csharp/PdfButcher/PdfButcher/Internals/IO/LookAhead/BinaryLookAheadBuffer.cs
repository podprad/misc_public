namespace PdfButcher.Internals.IO.LookAhead
{
    using System;
    using System.IO;

    /// <summary>
    /// Faster than list-oriented buffer.
    /// </summary>
    internal class BinaryLookAheadBuffer : ILookAheadReader<int>
    {
        // Usually PDF objects have length < 1024.
        // Such a buffer size guarantees that LookAhead() will not expand buffer too often.
        private const int BaseSize = 1024;

        private readonly Stream _stream;

        private byte[] _buffer;
        private int _bufferIndex;
        private int _bytesInBuffer;

        public BinaryLookAheadBuffer(Stream stream)
        {
            _stream = stream;

            _buffer = new byte[BaseSize];
        }

        public void Clear()
        {
            _bufferIndex = 0;
            _bytesInBuffer = 0;

            // Clear is called before start reading object.
            // We can shrink the buffer which was expanded during reading of previous object.
            if (_buffer.Length > BaseSize)
            {
                _buffer = new byte[BaseSize];
            }
        }

        public int AheadCount => _bytesInBuffer - _bufferIndex;

        public int Read()
        {
            if (_bytesInBuffer == 0 || _bufferIndex >= _bytesInBuffer)
            {
                _bytesInBuffer = _stream.Read(_buffer, 0, _buffer.Length);
                _bufferIndex = 0;

                if (_bytesInBuffer <= 0)
                {
                    return -1;
                }
            }

            return _buffer[_bufferIndex++];
        }

        public int LookAhead(int howFar = 1)
        {
            var lookIndex = _bufferIndex + (howFar - 1);
            if (lookIndex > _bytesInBuffer - 1)
            {
                var oldLength = _buffer.Length;
                Array.Resize(ref _buffer, oldLength + BaseSize);
                var readCount = _stream.Read(_buffer, oldLength, BaseSize);
                if (readCount == 0)
                {
                    return -1;
                }

                _bytesInBuffer += readCount;
            }

            return _buffer[lookIndex];
        }
    }
}