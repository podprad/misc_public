namespace PdfButcher.Internals.Model
{
    using System;
    using System.Globalization;
    using System.IO;
    using PdfButcher.Internals.IO;

    public class PdfInteger : PdfObject
    {
        public PdfInteger(long value)
        {
            Value = value;
        }

        public long Value { get; }

        public override bool TryResolveValue(IPdfIndirectObjectsResolver resolver, Type type, out object value)
        {
            if (type == typeof(long))
            {
                value = this.Value;

                return true;
            }

            if (type == typeof(int))
            {
                value = (int)this.Value;

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