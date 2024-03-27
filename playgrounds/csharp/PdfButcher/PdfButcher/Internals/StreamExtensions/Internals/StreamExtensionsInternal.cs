namespace PdfButcher.Internals.StreamExtensions.Internals
{
    using System.IO;

    internal static class StreamExtensionsInternal
    {
        public static bool TryReadByte(this Stream stream, out byte result)
        {
            var readInt = stream.ReadByte();
            if (readInt < 0)
            {
                result = 0;

                return false;
            }

            result = (byte)readInt;

            return true;
        }
    }
}