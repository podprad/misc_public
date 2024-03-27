namespace PdfButcher.Internals.IO.LookAhead
{
    using System;

    public class FuncLookAheadReader<T> : LookAheadReaderBase<T>
    {
        private readonly Func<T> _readNextFunc;

        public FuncLookAheadReader(Func<T> readNext)
        {
            _readNextFunc = readNext;
        }

        protected override T ReadNextCore()
        {
            return _readNextFunc();
        }
    }
}