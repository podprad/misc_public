namespace PdfButcher.Internals
{
    using System.Reflection;
    using System.Text;

    internal static class PdfConstants
    {
        public const int OptimalBufferSize = 32 * 1024;

        public const string Null = "null";

        public const string True = "true";

        public const string False = "false";

        public static readonly Encoding PdfFileEncoding = Encoding.GetEncoding("Windows-1252");

        public static readonly Assembly Assembly = typeof(PdfConstants).Assembly;

        public static readonly string ProductName = Assembly.GetName().Name;

        public static readonly string ProductVersion = Assembly.GetName().Version.ToString();

        public static readonly byte[] NullArray = PdfFileEncoding.GetBytes(Null);

        public static readonly byte[] TrueArray = PdfFileEncoding.GetBytes(True);

        public static readonly byte[] FalseArray = PdfFileEncoding.GetBytes(False);

        public static readonly byte WhiteSpaceByte = (byte)' ';

        public static readonly byte[] DictionaryStartArray = PdfFileEncoding.GetBytes("<<");

        public static readonly byte[] DictionaryEndArray = PdfFileEncoding.GetBytes(">>");

        public static readonly byte ArrayStartByte = (byte)'[';

        public static readonly byte ArrayEndByte = (byte)']';
    }
}