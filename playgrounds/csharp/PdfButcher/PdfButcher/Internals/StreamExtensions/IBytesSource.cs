namespace PdfButcher.Internals.StreamExtensions
{
    /// <summary>
    /// The interface allowing to search not only in streams.
    /// To use search in the stream use the extension: <see cref="StreamExtensions.ToBytesSource"/>.
    /// </summary>
    public interface IBytesSource
    {
        int Read(byte[] buffer, int offset, int count);
    }
}