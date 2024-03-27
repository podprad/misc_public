namespace PdfButcher.Internals.Model
{
    using System;
    using System.IO;
    using PdfButcher.Internals.IO;

    public class PdfName :
        PdfObject,
        IEquatable<PdfName>,
        IEquatable<string>
    {
        private readonly int _hash;

        public PdfName(string value)
        {
            Value = value ?? string.Empty;
            _hash = Value.GetHashCode();
        }

        public string Value { get; }

        public bool Equals(PdfName other)
        {
            return _hash == (other?.GetHashCode() ?? 0);
        }

        public bool Equals(string other)
        {
            return _hash == (other?.GetHashCode() ?? 0);
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            // Immutable
            return this;
        }

        public override void WriteTo(Stream stream)
        {
            EncodingHelper.EncodeName(Value, stream);
        }

        public override bool TryResolveValue(IPdfIndirectObjectsResolver resolver, Type type, out object value)
        {
            value = this.Value;

            return true;
        }
    }
}