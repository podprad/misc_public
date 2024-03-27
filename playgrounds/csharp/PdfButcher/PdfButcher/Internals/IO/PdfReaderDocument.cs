namespace PdfButcher.Internals.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.Model;

    /// <summary>
    /// Document initially parsed. Further parsing will be done while reading properties.
    /// </summary>
    public class PdfReaderDocument : IPdfDocument
    {
        private readonly PdfIndirectObjectsGroup _pdfIndirectObjectsGroup;
        private readonly Dictionary<long, PdfIndirectObject> _lazyPages;
        private readonly Lazy<PdfIndirectObject> _lazyPagesDictionary;
        private readonly Stream _stream;
        private readonly PdfReader _reader;
        private readonly bool _leaveStreamOpen;
        private readonly Lazy<long> _lazyPagesCount;

        public PdfReaderDocument(Stream stream, PdfReader reader, bool leaveStreamOpen)
        {
            _stream = stream;
            _reader = reader;
            _leaveStreamOpen = leaveStreamOpen;
            _lazyPagesDictionary = new Lazy<PdfIndirectObject>(GetPagesDictionary);
            _pdfIndirectObjectsGroup = new PdfIndirectObjectsGroup();
            _lazyPages = new Dictionary<long, PdfIndirectObject>();
            _lazyPagesCount = new Lazy<long>(() => Pages.PdfObject.AsOrThrow<PdfDictionary>().GetOrThrow(PdfNames.Count).ResolveValueOrThrow(this).AsOrThrow<PdfInteger>().Value);
        }

        public int Version { get; set; }

        public XRefTable XRefTable { get; } = new XRefTable();

        public PdfIndirectObject Pages => _lazyPagesDictionary.Value;

        public PdfIndirectObject Catalog { get; set; }

        public long PagesCount => _lazyPagesCount.Value;

        public PdfDictionary Trailer { get; set; }

        /// <summary>
        /// Exposed for testing purposes only.
        /// </summary>
        internal PdfReader Reader => _reader;

        public IEnumerable<PdfIndirectObject> GetPages()
        {
            var count = PagesCount;
            for (int i = 0; i < count; i++)
            {
                var page = GetPage(i);

                yield return page;
            }
        }

        public IEnumerable<PdfIndirectObject> GetIndirectObjects()
        {
            foreach (var xrefEntry in XRefTable.Entries)
            {
                if (xrefEntry.ObjectNumber < 1)
                {
                    continue;
                }

                var rootObject = this.GetIndirectObjectOrThrow(new PdfObjectId(xrefEntry.ObjectNumber, xrefEntry.Revision));

                yield return rootObject;
            }
        }

        public PdfIndirectObject GetPage(int index)
        {
            if (!_lazyPages.TryGetValue(index, out var lazyPage))
            {
                var kids = Pages.PdfObject.AsOrThrow<PdfDictionary>()
                    .GetOrThrow(PdfNames.Kids)
                    .ResolveValueOrThrow(this)
                    .AsOrThrow<PdfArray>();

                var kid = kids.Values.ElementAtOrDefault(index);
                if (kid == null)
                {
                    throw new PdfException($"No page kid found with index {index}");
                }

                var reference = kid.AsOrThrow<PdfReference>();
                lazyPage = this.GetIndirectObjectOrThrow(reference.ReferencedObjectId);
                _lazyPages[index] = lazyPage;
            }

            return lazyPage;
        }

        public PdfIndirectObject GetIndirectObjectOrNull(PdfObjectId pdfObjectId)
        {
            var indirectObject = _pdfIndirectObjectsGroup.GetIndirectObjectOrNull(pdfObjectId);

            if (indirectObject == null)
            {
                // TODO: Add support for broken documents and not throw.
                var xRef = XRefTable.GetXRefOrThrow(pdfObjectId.Id);
                if (xRef.Free)
                {
                    const bool Removed = true;
                    return new PdfIndirectObject(new PdfObjectId(xRef.ObjectNumber, xRef.Revision), Removed);
                }

                if (xRef.IsInStream)
                {
                    var parentXRef = XRefTable.GetXRefOrThrow(xRef.StreamObjectNumber);
                    var streamObject = _reader.ReadIndirectObjectAtOffset(parentXRef.Offset);
                    _pdfIndirectObjectsGroup.Add(streamObject);

                    foreach(var packedObject in _reader.ReadIndirectObjectsFromObjectsStream(this, streamObject))
                    {
                        _pdfIndirectObjectsGroup.Add(packedObject);
                    }

                    return _pdfIndirectObjectsGroup.GetIndirectObjectOrNull(pdfObjectId);
                }
                else
                {
                    indirectObject = _reader.ReadIndirectObjectAtOffset(xRef.Offset);
                    _pdfIndirectObjectsGroup.Add(indirectObject);
                }
            }

            return indirectObject;
        }

        public void Dispose()
        {
            if (!_leaveStreamOpen)
            {
                _stream?.Dispose();
            }
        }

        private PdfIndirectObject GetPagesDictionary()
        {
            // TODO: Add support for broken documents and not throw.
            var pagesReference = Catalog.PdfObject.AsOrThrow<PdfDictionary>().GetOrThrow(PdfNames.Pages).AsOrThrow<PdfReference>();
            var indirectObject = this.GetIndirectObjectOrThrow(pagesReference.ReferencedObjectId);

            return indirectObject;
        }
    }
}