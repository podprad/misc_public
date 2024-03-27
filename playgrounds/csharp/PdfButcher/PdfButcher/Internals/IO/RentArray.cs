namespace PdfButcher.Internals.IO
{
    using System;
    using System.Buffers;

    public class RentArray<T> : IDisposable
    {
        private readonly T[] _array;
        private int _length;
        private bool _disposed;

        public RentArray(int length)
        {
            _array = ArrayPool<T>.Shared.Rent(length);
            _length = length;
        }

        public T[] Array
        {
            get
            {
                CheckDisposed();

                return _array;
            }
        }

        public int Length
        {
            get
            {
                CheckDisposed();

                return _length;
            }
        }

        public void Dispose()
        {
            _disposed = true;
            ArrayPool<T>.Shared.Return(_array);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RentArray<T>));
            }
        }
    }
}