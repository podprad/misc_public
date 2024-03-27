namespace PdfButcher.Internals.Model
{
    using System;
    using System.Globalization;
    using System.IO;
    using PdfButcher.Internals.IO;

    public class PdfReal : PdfObject
    {
        public PdfReal(decimal value)
        {
            this.Value = value;
        }

        public decimal Value { get; }

        public override bool TryResolveValue(IPdfIndirectObjectsResolver resolver, Type type, out object value)
        {
            if (type == typeof(decimal))
            {
                value = Value;

                return true;
            }

            value = null;

            return false;
        }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            // Immutable
            return this;
        }

        public override void WriteTo(Stream stream)
        {
            var stringValue = Value.ToString(CultureInfo.InvariantCulture);
            EncodingHelper.RawWrite(stream, stringValue);
        }
    }
}