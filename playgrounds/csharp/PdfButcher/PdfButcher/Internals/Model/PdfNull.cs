namespace PdfButcher.Internals.Model
{
    using System.IO;

    public class PdfNull : PdfObject
    {
        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            // Immutable
            return this;
        }

        public override void WriteTo(Stream stream)
        {
            stream.Write(PdfConstants.NullArray, 0, PdfConstants.NullArray.Length);
        }
    }
}