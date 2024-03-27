namespace PdfButcher.Benchmarks.StreamReading
{
    using System.IO;
    using System.Runtime.CompilerServices;

    internal static class StaticByteReader
    {
        public static void InitRead(out int bufferPosition, out int bufferFill, out byte[] buffer, int bufferLength)
        {
            bufferPosition = -1;
            bufferFill = -1;
            buffer = new byte[bufferLength];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadByte(Stream stream, ref int bufferPosition, ref int bufferFill, byte[] buffer)
        {
            if (bufferPosition > bufferFill - 1 || bufferPosition < 0)
            {
                bufferFill = stream.Read(buffer, 0, buffer.Length);
                bufferPosition = 0;

                if (bufferFill == 0)
                {
                    return -1;
                }
            }

            return buffer[bufferPosition++];
        }
    }
}