namespace PdfButcher.Internals
{
    using PdfButcher.Internals.Model;

    public interface IPdfIndirectObjectsResolver
    {
        PdfIndirectObject GetIndirectObjectOrNull(PdfObjectId pdfObjectId);
    }
}