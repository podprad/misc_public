namespace PdfButcher.Internals.StreamExtensions
{
    using System.IO;
    using PdfButcher.Internals.StreamExtensions.Internals;

    public static class StreamExtensions
    {
        /// <summary>
        /// Converts <see cref="Stream"/> to <see cref="IBytesSource"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The bytes sources.</returns>
        public static IBytesSource ToBytesSource(this Stream stream)
        {
            return new StreamBytesSource(stream);
        }
    }
}