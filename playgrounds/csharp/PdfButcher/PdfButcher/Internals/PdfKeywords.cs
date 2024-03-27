namespace PdfButcher.Internals
{
    using PdfButcher.Internals.IO;

    public static class PdfKeywords
    {
        public const string StartXRef = "startxref";

        public const string XRef = "xref";

        public const string F = "f";

        public const string N = "n";

        public const string Trailer = "trailer";

        public const string Obj = "obj";

        public const string EndObj = "endobj";

        public const string Stream = "stream";

        public const string EndStream = "endstream";

        public const string R = "R";

        public static readonly byte[] StartXRefBytes = EncodingHelper.RawEncode(StartXRef);

        public static readonly byte[] XRefBytes = EncodingHelper.RawEncode(XRef);
    }
}