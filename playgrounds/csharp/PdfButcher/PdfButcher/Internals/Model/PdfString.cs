namespace PdfButcher.Internals.Model
{
    using System.IO;
    using PdfButcher.Internals.IO;

    public class PdfString : PdfObject
    {
        public PdfString(string value)
        {
            Value = value;

            using (var buffer = new MemoryStream())
            {
                EncodingHelper.EncodeLiteralString(value, buffer);
                RawValue = buffer.ToArray();
            }
        }

        public PdfString(byte[] rawValue)
        {
            RawValue = rawValue;
            Value = EncodingHelper.DecodeLiteralString(RawValue, RawValue.Length);
        }

        public string Value { get; }
        
        public byte[] RawValue { get; }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            // Immutable
            return this;
        }

        public override void WriteTo(Stream stream)
        {
            stream.Write(RawValue, 0, RawValue.Length);
        }
    }
}