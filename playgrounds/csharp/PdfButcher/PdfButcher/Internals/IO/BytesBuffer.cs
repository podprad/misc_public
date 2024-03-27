namespace PdfButcher.Internals.IO
{
    using System;

    /// <summary>
    /// No performance impact when having this as separate class.
    /// </summary>
    internal class BytesBuffer
    {
        private const int BufferSizeIncrement = 1024;

        private byte[] _buffer;
        private int _bufferSize;
        private int _bufferCount;

        public BytesBuffer()
        {
            _bufferSize = BufferSizeIncrement;
            _buffer = new byte[_bufferSize];
            _bufferCount = 0;
        }

        public byte[] Buffer => _buffer;

        public int Length => _bufferCount;

        public void Clear()
        {
            _bufferCount = 0;
        }

        public void Append(byte b)
        {
            if (_bufferSize < _bufferCount + 1)
            {
                _bufferSize += BufferSizeIncrement;
                Array.Resize(ref _buffer, _bufferSize);
            }

            _buffer[_bufferCount] = b;
            _bufferCount++;
        }
    }
}