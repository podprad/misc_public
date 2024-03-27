namespace PdfButcher.Internals.StreamExtensions.Internals
{
    internal static class NaiveSearch
    {
        public static long IndexOf(IBytesSource bytesSource, byte[] searchTerm)
        {
            var frame = new Frame(bytesSource, searchTerm.Length, searchTerm.Length);

            while (true)
            {
                if (frame.EndOfFile())
                {
                    return -1;
                }

                if (frame.FrameEquals(searchTerm))
                {
                    return frame.AbsolutePosition;
                }

                frame.ShiftRight(1);
            }
        }
    }
}