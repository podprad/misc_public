namespace PdfButcher.Internals.StreamExtensions.Internals
{
    using System;
    using System.Runtime.CompilerServices;

    //// Input:
    //// ..............................ABC................ABC............
    //// |__________32 bytes buffer 1____|__________32 bytes buffer 2____|
    //// 3 bytes frame, example: shifting right by 3 bytes
    //// |_|...|_|...|_|...|_|...|_|...|_|...|_|...|_|...|_|...|_|...|_|...|_|...
    ////   --->  --->  --->  --->  --->  --->  --->  --->  --->  --->  --->  --->...

    /// <summary>
    /// Allows to reduce amount of stream.Read() calls.
    /// </summary>
    internal class Frame
    {
        private readonly IBytesSource _bytesSource;
        private readonly int _frameSize;
        private readonly int _bufferSize;

        private int _framePosition;
        private long _frameAbsolutePosition;

        private BufferAndFill _buffer1;
        private BufferAndFill _buffer2;

        // AggressiveInlining because of: Fail Reason: too many il bytes
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Frame(IBytesSource bytesSource, int frameSize, int bufferSize)
        {
            if (bufferSize < frameSize)
            {
                throw new InvalidOperationException("TODO");
            }

            _bytesSource = bytesSource;
            _frameSize = frameSize;
            _bufferSize = bufferSize;
            _framePosition = 0;
            _frameAbsolutePosition = 0;

            _buffer1 = new BufferAndFill(bufferSize);
            _buffer2 = new BufferAndFill(bufferSize);

            _buffer1.Load(bytesSource);
            _buffer2.Load(bytesSource);
        }

        public long AbsolutePosition => _frameAbsolutePosition;

        internal static int AdjustBufferSize(int frameSize, int bufferSize)
        {
            if (frameSize <= bufferSize)
            {
                return bufferSize;
            }

            var multiplier = frameSize / bufferSize;
            var newBufferSize = (multiplier + 1) * bufferSize;

            return newBufferSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndOfFile()
        {
            if (!TryGetByte(_frameSize - 1, out _))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetByte(int frameRelativeIndex, out byte b)
        {
            // 0 <= index < _frameSize
            var position = _framePosition + frameRelativeIndex;

            if (position < _bufferSize)
            {
                if (position < _buffer1.Fill)
                {
                    b = _buffer1.Buffer[position];

                    return true;
                }

                b = 0;

                return false;
            }
            else
            {
                position -= _bufferSize;

                if (position < _buffer2.Fill)
                {
                    b = _buffer2.Buffer[position];

                    return true;
                }

                b = 0;

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ShiftRight(int count)
        {
            _framePosition += count;
            _frameAbsolutePosition += count;

            if (_framePosition > _bufferSize)
            {
                SwapBuffers();
                _buffer2.Load(_bytesSource);

                _framePosition -= _bufferSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FrameEquals(byte[] sequence)
        {
            var to = Math.Min(sequence.Length, _frameSize);
            for (int i = 0; i < to; i++)
            {
                if (TryGetByte(i, out var b) && b == sequence[i])
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SwapBuffers()
        {
            (_buffer1, _buffer2) = (_buffer2, _buffer1);
        }

        private class BufferAndFill
        {
            public BufferAndFill(int bufferSize)
            {
                Buffer = new byte[bufferSize];
            }

            public byte[] Buffer { get; set; }

            public int Fill { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Load(IBytesSource bytesSource)
            {
                Fill = bytesSource.Read(Buffer, 0, Buffer.Length);
            }
        }
    }
}