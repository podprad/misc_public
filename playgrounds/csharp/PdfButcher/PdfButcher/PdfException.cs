namespace PdfButcher
{
    using System;

    public class PdfException : Exception
    {
        public PdfException(string message)
            : base(message)
        {
        }
    }
}