namespace PdfButcher.Internals
{
    using System;
    using System.Collections.Generic;
    using PdfButcher.Internals.Model;

    public interface IPdfDocument : IPdfIndirectObjectsResolver, IDisposable
    {
        int Version { get; }

        XRefTable XRefTable { get; }

        PdfIndirectObject Pages { get; }

        PdfIndirectObject Catalog { get; }

        long PagesCount { get; }

        PdfDictionary Trailer { get; }

        IEnumerable<PdfIndirectObject> GetPages();

        PdfIndirectObject GetPage(int index);
    }
}