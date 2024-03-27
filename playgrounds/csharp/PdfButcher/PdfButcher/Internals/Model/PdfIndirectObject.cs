namespace PdfButcher.Internals.Model
{
    /// <summary>
    /// Object that lives in root PDF structure, has id and might have stream.
    /// </summary>
    public class PdfIndirectObject
    {
        public PdfIndirectObject(PdfObjectId id, bool removed)
            : this(id, null, null, removed)
        {
        }

        public PdfIndirectObject(PdfObjectId id, PdfObject pdfObject)
            : this(id, pdfObject, null)
        {
        }

        public PdfIndirectObject(PdfObjectId id, PdfObject pdfObject, PdfStream stream)
            : this(id, pdfObject, stream, false)
        {
        }

        public PdfIndirectObject(PdfObjectId id, PdfObject pdfObject, PdfStream stream, bool removed)
        {
            Id = id;
            PdfObject = pdfObject;
            PdfStream = stream;
            Removed = removed;
        }

        public PdfObjectId Id { get; set; }

        public PdfObject PdfObject { get; }

        public PdfStream PdfStream { get; }

        public bool Removed { get; }

        public PdfIndirectObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            if (this.Removed)
            {
                return new PdfIndirectObject(Id, true);
            }

            var pdfObjectClone = PdfObject.Clone(resolver);
            var pdfStreamClone = PdfStream?.Clone(pdfObjectClone.AsOrThrow<PdfDictionary>(), resolver);

            return new PdfIndirectObject(Id, pdfObjectClone, pdfStreamClone);
        }

        public override string ToString()
        {
            return $"Id={Id}, Stream={PdfStream != null}, Object={PdfObject} {(Removed ? "Removed" : "")}";
        }
    }
}