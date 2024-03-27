namespace PdfButcher.Internals.IO.LookAhead
{
    using System;
    using System.Collections.Generic;

    public abstract class LookAheadReaderBase<T> : ILookAheadReader<T>
    {
        private readonly List<T> _ahead = new List<T>();

        public int AheadCount => _ahead.Count;

        public void Clear()
        {
            _ahead.Clear();
        }

        public T LookAhead(int howFar = 1)
        {
            var requiredReads = Math.Max(howFar - _ahead.Count, 0);

            for (int i = 0; i < requiredReads; i++)
            {
                var next = ReadNextCore();
                _ahead.Add(next);
            }

            return _ahead[howFar - 1];
        }

        public T Read()
        {
            if (_ahead.Count > 0)
            {
                var first = _ahead[0];
                _ahead.RemoveAt(0);

                return first;
            }

            return ReadNextCore();
        }

        protected abstract T ReadNextCore();
    }
}