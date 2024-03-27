namespace PdfButcher.Internals.IO.LookAhead
{
    public interface ILookAheadReader<T>
    {
        int AheadCount { get; }

        void Clear();

        T LookAhead(int howFar = 1);

        T Read();
    }
}