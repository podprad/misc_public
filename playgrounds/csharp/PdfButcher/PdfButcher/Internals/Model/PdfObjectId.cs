namespace PdfButcher.Internals.Model
{
    using System;

    public struct PdfObjectId : IEquatable<PdfObjectId>
    {
        private readonly string _stringValue;
        private readonly int _hash;

        public PdfObjectId(long id, long revision)
        {
            Id = id;
            Revision = revision;

            _stringValue = id + " " + revision;
            _hash = _stringValue.GetHashCode();
        }

        public long Id { get; }

        public long Revision { get; }

        public bool Equals(PdfObjectId other)
        {
            return Id == other.Id && Revision == other.Revision;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        public override string ToString()
        {
            return _stringValue;
        }
    }
}