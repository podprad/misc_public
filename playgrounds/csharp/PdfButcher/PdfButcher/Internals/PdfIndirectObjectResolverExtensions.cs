namespace PdfButcher.Internals
{
    using PdfButcher.Internals.Model;

    public static class PdfIndirectObjectResolverExtensions
    {
        public static PdfIndirectObject GetIndirectObjectOrThrow(this IPdfIndirectObjectsResolver resolver, PdfObjectId pdfObjectId)
        {
            var result = resolver.GetIndirectObjectOrNull(pdfObjectId);
            if (result == null)
            {
                throw new PdfException($"{pdfObjectId} not found in {resolver}");
            }

            return result;
        }
    }
}