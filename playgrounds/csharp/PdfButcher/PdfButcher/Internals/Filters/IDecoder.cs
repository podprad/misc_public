namespace PdfButcher.Internals.Filters
{
    using System.IO;
    using PdfButcher.Internals.Model;

    public interface IDecoder
    {
        Stream Decode(Stream stream, IPdfIndirectObjectsResolver pdfIndirectObjectsResolver, PdfDictionary filterParameters);
    }
}