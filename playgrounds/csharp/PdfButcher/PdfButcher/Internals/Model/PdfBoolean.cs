namespace PdfButcher.Internals.Model
{
    using System.IO;

    public class PdfBoolean : PdfObject
    {
        public PdfBoolean(bool value)
        {
            Value = value;
        }

        public bool Value { get; }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            // Immutable
            return this;
        }

        public override void WriteTo(Stream stream)
        {
            var bytes = Value ? PdfConstants.TrueArray : PdfConstants.FalseArray;
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}