namespace PdfButcher.Internals.Model
{
    using System.Collections.Generic;
    using PdfButcher.Internals.Common;

    /// <summary>
    /// Responsible for grouping related indirect objects to let allow such operations as rename.
    /// </summary>
    public class PdfIndirectObjectsGroup : IPdfIndirectObjectsResolver
    {
        private readonly ByAddOrderMap<long, PdfIndirectObject> _map = new ByAddOrderMap<long, PdfIndirectObject>();

        public PdfIndirectObjectsGroup()
        {
        }

        public PdfIndirectObjectsGroup(IEnumerable<PdfIndirectObject> pdfIndirectObjects)
        {
            AddRange(pdfIndirectObjects);
        }

        public IEnumerable<PdfIndirectObject> ValuesInOrder => _map.ValuesInOrder;

        public IEnumerable<PdfIndirectObject> Values => _map.Values;

        /// <summary>
        /// Updates object ids where (old id) => (object with new id).
        /// </summary>
        public void ChangeIds(Dictionary<long, long> updates)
        {
            _map.ChangeKeys(updates);
        }

        public void Add(PdfIndirectObject pdfIndirectObject)
        {
            _map.Add(pdfIndirectObject.Id.Id, pdfIndirectObject);
        }

        public void Remove(PdfObjectId pdfObjectId)
        {
            _map.Remove(pdfObjectId.Id);
        }

        public void AddRange(IEnumerable<PdfIndirectObject> pdfIndirectObjects)
        {
            foreach (var pdfIndirectObject in pdfIndirectObjects)
            {
                Add(pdfIndirectObject);
            }
        }

        public PdfIndirectObject GetIndirectObjectOrNull(PdfObjectId pdfObjectId)
        {
            return _map.TryGetValue(pdfObjectId.Id, out var result) ? result : null;
        }
    }
}