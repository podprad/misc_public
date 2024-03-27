namespace PdfButcher.Internals.Model
{
    using System.Collections.Generic;
    using System.Linq;

    public class XRefTable
    {
        private readonly Dictionary<long, XRefEntry> _lookup = new Dictionary<long, XRefEntry>();

        public IEnumerable<XRefEntry> Entries => _lookup.Values.AsEnumerable();

        public int Count => _lookup.Count;

        public void AddIfNotExists(XRefEntry xRefEntry)
        {
            if (!_lookup.ContainsKey(xRefEntry.ObjectNumber))
            {
                _lookup.Add(xRefEntry.ObjectNumber, xRefEntry);
            }
        }

        public void AddIfNotExists(IEnumerable<XRefEntry> xRefEntries)
        {
            foreach (var entry in xRefEntries)
            {
                AddIfNotExists(entry);
            }
        }

        public long GetMaxObjectNumber()
        {
            return _lookup.Values.Max(g => g.ObjectNumber);
        }

        public XRefEntry GetXRefOrThrow(long objectId)
        {
            var xref = GetXRef(objectId);
            if (xref == null)
            {
                throw new PdfException($"Could not find xref entry for object {objectId}");
            }

            return xref;
        }

        public XRefEntry GetXRef(long objectId)
        {
            if (!_lookup.TryGetValue(objectId, out var xRef))
            {
                return null;
            }

            return xRef;
        }

        public IEnumerable<List<XRefEntry>> CreateWriteGroups()
        {
            var orderedList = _lookup.Values.OrderBy(g => g.ObjectNumber).ThenBy(g => g.Revision).ToList();

            var currentXRefGroup = new List<XRefEntry>();

            for (var i = 0; i < Count; i++)
            {
                var current = orderedList[i];
                var next = orderedList.ElementAtOrDefault(i + 1);

                currentXRefGroup.Add(current);

                if (next == null || next.ObjectNumber - 1 != current.ObjectNumber)
                {
                    yield return currentXRefGroup;
                    currentXRefGroup = new List<XRefEntry>();
                }
            }
        }

        public void Clear()
        {
            _lookup.Clear();
        }
    }
}