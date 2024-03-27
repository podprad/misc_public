namespace PdfButcher.Internals.Model
{
    using System;
    using System.IO;
    using PdfButcher.Internals.IO;

    public class PdfReference : PdfObject
    {
        public PdfReference(PdfObjectId referencedObjectIdId)
        {
            ReferencedObjectId = referencedObjectIdId;
        }

        public PdfObjectId ReferencedObjectId { get; set; }

        public override PdfObject ResolveValueOrThrow(IPdfIndirectObjectsResolver resolver)
        {
            var indirectObject = resolver.GetIndirectObjectOrThrow(this.ReferencedObjectId);

            return indirectObject.PdfObject;
        }

        public override bool TryResolveValue(IPdfIndirectObjectsResolver resolver, Type type, out object value)
        {
            return ResolveValueOrThrow(resolver).TryResolveValue(resolver, type, out value);
        }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            return new PdfReference(ReferencedObjectId);
        }

        public override void WriteTo(Stream stream)
        {
            var stringValue = ReferencedObjectId + " R";
            EncodingHelper.RawWrite(stream, stringValue);
        }
    }
}