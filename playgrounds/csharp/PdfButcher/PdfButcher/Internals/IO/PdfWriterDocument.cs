namespace PdfButcher.Internals.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PdfButcher.Internals.Common;
    using PdfButcher.Internals.Model;

    public class PdfWriterDocument : IPdfDocument
    {
        private readonly ByAddOrderMap<PdfObjectId, PdfWriterObject> _unwrittenObjects = new ByAddOrderMap<PdfObjectId, PdfWriterObject>();
        private readonly Dictionary<int, PdfObjectId> _pages = new Dictionary<int, PdfObjectId>();
        private readonly PdfWriterObject _catalogPdfWriterObject;
        private readonly PdfWriterObject _pagesPdfWriterObject;

        private int _totalPages = 0;

        private PdfObjectId _maxId;

        public PdfWriterDocument()
        {
            var pagesDictionary = new PdfDictionary();
            pagesDictionary.Set(PdfNames.Type, new PdfName(PdfNames.Pages));

            var kidsArray = new PdfArray();
            pagesDictionary.Set(PdfNames.Kids, kidsArray);
            pagesDictionary.Set(PdfNames.Count, new PdfInteger(kidsArray.Count));

            var pagesObject = new PdfIndirectObject(GetNextId(), pagesDictionary);
            _pagesPdfWriterObject = new PdfWriterObject(this, pagesObject);

            var catalogDictionary = new PdfDictionary();
            catalogDictionary.Set(PdfNames.Type, new PdfName(PdfNames.Catalog));
            catalogDictionary.Set(PdfNames.Pages, new PdfReference(Pages.Id));

            var catalogObject = new PdfIndirectObject(GetNextId(), catalogDictionary);
            _catalogPdfWriterObject = new PdfWriterObject(this, catalogObject);

            Trailer = new PdfDictionary();
            Trailer.Set(PdfNames.Root, new PdfReference(Catalog.Id));

            XRefTable.Clear();
            XRefTable.AddIfNotExists(new XRefEntry
            {
                Free = true,
                Offset = 0,
                Revision = 65535,
            });
        }

        public int Version { get; set; }

        public XRefTable XRefTable { get; } = new XRefTable();

        public PdfIndirectObject Pages => _pagesPdfWriterObject.GetValue();

        public PdfIndirectObject Catalog => _catalogPdfWriterObject.GetValue();

        public PdfDictionary Trailer { get; }

        public long PagesCount => _totalPages;

        public void Dispose()
        {
        }

        public IEnumerable<PdfIndirectObject> GetPages()
        {
            throw new NotSupportedException();
        }

        public IEnumerable<PdfIndirectObject> GetUnwrittenObjects()
        {
            return _unwrittenObjects.ValuesInOrder.Select(g => g.GetValue());
        }

        public void AddObjects(IEnumerable<PdfIndirectObject> pdfObjects)
        {
            var group = new PdfIndirectObjectsGroup(pdfObjects);

            // Investigate
            var pageReferencesInOrder = new List<PdfReference>();
            var ignoreObjects = new List<PdfIndirectObject>();
            foreach (var objectToAdd in group.ValuesInOrder)
            {
                var currentDictionary = objectToAdd.PdfObject.AsOrNull<PdfDictionary>();
                if (currentDictionary != null)
                {
                    var objectType = currentDictionary.GetAndResolveValueOrDefault(group, PdfNames.Type, "");
                    switch (objectType)
                    {
                        case PdfNames.Catalog:
                            // We build own catalog.
                            ignoreObjects.Add(objectToAdd);

                            continue;

                        case PdfNames.Pages:
                        {
                            // We maintain own pages list, skip this object.
                            // Read kids to save pages in correct order.
                            var kidsArray = currentDictionary.Get(PdfNames.Kids).AsOrNull<PdfArray>();
                            if (kidsArray != null)
                            {
                                pageReferencesInOrder.AddRange(kidsArray.Values.OfType<PdfReference>());
                            }

                            ignoreObjects.Add(objectToAdd);

                            continue;
                        }

                        case PdfNames.XRef:
                            // We already have xref children unpacked.
                            // The owner/parent is not needed anymore.

                            ignoreObjects.Add(objectToAdd);

                            continue;
                    }
                }
            }

            // Assign new ids to objects that will not be removed.
            var renumberTool = new PdfObjectsRenumberTool();
            var objectsToRenumber = group.Values.Where(g => !ignoreObjects.Contains(g)).ToList();
            var renumberStartFrom = GetNextId();
            _maxId = renumberTool.RenumberObjects(group, renumberStartFrom, objectsToRenumber);

            // First prepare pages by order.
            var objectsToAdd = new ByAddOrderMap<PdfObjectId, PdfIndirectObject>();
            foreach (var pdfReference in pageReferencesInOrder)
            {
                var page = group.GetIndirectObjectOrThrow(pdfReference.ReferencedObjectId);
                objectsToAdd.Add(page.Id, page);

                // Set new parent.
                page.PdfObject.AsOrThrow<PdfDictionary>().Set(PdfNames.Parent, new PdfReference(Pages.Id));
            }

            // Prepare rest of objects.
            foreach (var indirectObject in group.ValuesInOrder)
            {
                if (!objectsToAdd.ContainsKey(indirectObject.Id))
                {
                    objectsToAdd.Add(indirectObject.Id, indirectObject);
                }
            }

            // Add objects.
            // TODO: Consider not adding unreferenced objects.
            var addedObjects = new List<PdfWriterObject>();
            foreach (var objectToAdd in objectsToAdd)
            {
                if (!ignoreObjects.Contains(objectToAdd))
                {
                    var writerObject = new PdfWriterObject(this, objectToAdd);
                    _unwrittenObjects.Add(writerObject.ObjectId, writerObject);
                    addedObjects.Add(writerObject);
                }
            }

            // Update infos, there might be references between objects.
            foreach (var writeObject in addedObjects)
            {
                writeObject.Load();

                if (writeObject.IsPage)
                {
                    _pages[_totalPages] = writeObject.ObjectId;
                    _totalPages++;
                }
            }

            // Merge ignored objects like /Catalog, it might contain info like metadata.
            foreach (var ignoreObject in ignoreObjects)
            {
                var dictionary = ignoreObject.PdfObject.AsOrNull<PdfDictionary>();
                if (dictionary != null)
                {
                    var typeValue = dictionary.Get(PdfNames.Type)?.ResolveValueOrThrow(group).AsOrNull<PdfName>()?.Value;
                    if (typeValue == PdfNames.Catalog)
                    {
                        Catalog.PdfObject.AsOrThrow<PdfDictionary>().MergeNotExisting(dictionary);
                    }

                    if (typeValue == PdfNames.Pages)
                    {
                        Pages.PdfObject.AsOrNull<PdfDictionary>().MergeNotExisting(dictionary);
                    }

                    // We ignored XRef, but it might contain useful information like ref to Info object.
                    if (typeValue == PdfNames.XRef)
                    {
                        Trailer.MergeSelected(dictionary, false, PdfNames.Info);
                    }
                }
            }
        }

        public PdfIndirectObject GetPage(int index)
        {
            throw new NotSupportedException();
        }

        public PdfIndirectObject GetIndirectObjectOrNull(PdfObjectId pdfObjectId)
        {
            return _unwrittenObjects.TryGetValue(pdfObjectId, out var pdfObject) ? pdfObject.GetValue() : null;
        }

        public void MarkAsWritten(PdfObjectId pdfObjectId, long position)
        {
            _unwrittenObjects.Remove(pdfObjectId);

            if (position < 0)
            {
                XRefTable.AddIfNotExists(new XRefEntry()
                {
                    Free = true,
                    Offset = pdfObjectId.Id,
                    Revision = 65535,
                    ObjectNumber = pdfObjectId.Id,
                });
            }
            else
            {
                XRefTable.AddIfNotExists(new XRefEntry
                {
                    Free = false,
                    Offset = position,
                    Revision = pdfObjectId.Revision,
                    ObjectNumber = pdfObjectId.Id,
                });
            }
        }

        public void UpdatePagesDictionary()
        {
            var references = _pages.OrderBy(g => g.Key).Select(p => new PdfReference(p.Value));
            var kidsArray = new PdfArray(references);
            Pages.PdfObject.AsOrThrow<PdfDictionary>().Set(PdfNames.Kids, kidsArray);
            Pages.PdfObject.AsOrThrow<PdfDictionary>().Set(PdfNames.Count, new PdfInteger(kidsArray.Count));
        }

        public void UpdateTrailer()
        {
            // ISO, 7.5.5 File trailer
            // Size, integer, (Required; shall not be an indirect reference) The total number of entries
            // in the PDF file’s cross-reference table, as defined by the combination of
            // the original section and all update sections. Equivalently, this value shall
            // be 1 greater than the highest object number defined in the PDF file.
            // Any object in a cross-reference section whose number is greater than 
            // this value shall be ignored and defined to be missing by a PDF reader.

            // Size should be set to max + 1.
            var max = XRefTable.GetMaxObjectNumber();
            Trailer.Set(PdfNames.Size, new PdfInteger(max + 1));
        }

        private PdfObjectId GetNextId()
        {
            var nextObjectId = new PdfObjectId(_maxId.Id + 1, 0);
            _maxId = nextObjectId;

            return _maxId;
        }

        private class PdfWriterObject
        {
            private readonly PdfWriterDocument _document;
            private PdfIndirectObject _pdfObject;

            public PdfWriterObject(PdfWriterDocument document, PdfIndirectObject pdfObject)
            {
                _document = document;
                _pdfObject = pdfObject;

                ObjectId = pdfObject.Id;
            }

            public PdfObjectId ObjectId { get; set; }

            public string PdfType { get; set; }

            public bool IsPage => PdfType == PdfNames.Page;

            public bool IsLoaded { get; private set; }

            public PdfIndirectObject GetValue()
            {
                return _pdfObject;
            }

            public override string ToString()
            {
                return $"{_pdfObject}";
            }

            public void Load()
            {
                if (!IsLoaded)
                {
                    if (_pdfObject.PdfObject is PdfDictionary pdfDictionary)
                    {
                        var pdfObjectType = pdfDictionary.Get(PdfNames.Type)?.ResolveValueOrThrow(_document).AsOrThrow<PdfName>();
                        this.PdfType = pdfObjectType?.Value;
                    }

                    IsLoaded = true;
                }
            }
        }
    }
}