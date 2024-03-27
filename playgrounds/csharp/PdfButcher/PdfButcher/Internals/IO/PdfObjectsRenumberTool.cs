namespace PdfButcher.Internals.IO
{
    using System.Collections.Generic;
    using System.Linq;
    using PdfButcher.Internals.Common;
    using PdfButcher.Internals.Model;

    public class PdfObjectsRenumberTool
    {
        /// <summary>
        /// Renumber objects and returns last written id.
        /// </summary>
        public PdfObjectId RenumberObjects(PdfIndirectObjectsGroup group, PdfObjectId startId, IEnumerable<PdfIndirectObject> objectsToRenumber)
        {
            var buckets = MakeBuckets(group);

            var changesMap = new Dictionary<long, long>();

            var next = startId.Id;

            var reorderItems = new ByAddOrderMap<long, PdfIndirectObject>();

            // Add wanted items
            foreach (var indirectObject in objectsToRenumber)
            {
                reorderItems.Add(indirectObject.Id.Id, indirectObject);
            }

            // Add rest of the items.
            foreach (var indirectObject in group.ValuesInOrder)
            {
                if (!reorderItems.ContainsKey(indirectObject.Id.Id))
                {
                    reorderItems.Add(indirectObject.Id.Id, indirectObject);
                }
            }

            // Update references.
            foreach (var indirectObject in reorderItems.Values)
            {
                var oldId = indirectObject.Id;
                var newId = new PdfObjectId(next, 0);
                changesMap[oldId.Id] = newId.Id;

                var bucket = buckets.GetValueOrThrow(oldId.Id);
                foreach (var reference in bucket)
                {
                    reference.ReferencedObjectId = newId;
                }

                indirectObject.Id = newId;

                next++;
            }

            // Update info
            group.ChangeIds(changesMap);

            return new PdfObjectId(next - 1, 0);
        }

        private ByAddOrderMap<long, List<PdfReference>> MakeBuckets(PdfIndirectObjectsGroup group)
        {
            var buckets = new ByAddOrderMap<long, List<PdfReference>>();

            foreach (var indirectObject in group.Values)
            {
                buckets.Add(indirectObject.Id.Id, new List<PdfReference>());
            }

            var allReferences = group.Values.SelectMany(g => g.PdfObject.Flatten(true))
                .OfType<PdfReference>();

            foreach (var reference in allReferences)
            {
                var bucket = buckets.GetValueOrThrow(reference.ReferencedObjectId.Id);
                bucket.Add(reference);
            }

            return buckets;
        }
    }
}