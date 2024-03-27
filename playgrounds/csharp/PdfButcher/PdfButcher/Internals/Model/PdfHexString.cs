namespace PdfButcher.Internals.Model
{
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.IO;

    public class PdfHexString : PdfObject
    {
        public PdfHexString(byte[] characters)
        {
            this.Characters = characters;
        }

        public byte[] Characters { get; }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            return new PdfHexString(Characters.ToArray());
        }

        public override void WriteTo(Stream stream)
        {
            var stringValue = EncodingHelper.EncodeHexString(Characters);
            EncodingHelper.RawWrite(stream, stringValue);
        }
    }
}