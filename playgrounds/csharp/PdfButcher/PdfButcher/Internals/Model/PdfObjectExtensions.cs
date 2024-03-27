namespace PdfButcher.Internals.Model
{
    internal static class PdfObjectExtensions
    {
        public static TPdfObject AsOrNull<TPdfObject>(this PdfObject pdfObject)
            where TPdfObject : PdfObject
        {
            if (pdfObject is TPdfObject tPdfObject)
            {
                return tPdfObject;
            }

            return null;
        }

        public static TPdfObject AsOrThrow<TPdfObject>(this PdfObject pdfObject)
            where TPdfObject : PdfObject
        {
            if (pdfObject is TPdfObject tPdfObject)
            {
                return tPdfObject;
            }

            throw new PdfException($"Expected object {pdfObject?.GetType()} to be {typeof(TPdfObject)}.");
        }

        public static bool Is<TPdfObject>(this PdfObject pdfObject)
            where TPdfObject : PdfObject
        {
            return pdfObject is TPdfObject;
        }
    }
}