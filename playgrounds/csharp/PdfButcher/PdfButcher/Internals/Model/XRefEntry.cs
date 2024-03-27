namespace PdfButcher.Internals.Model
{
    using System.Diagnostics;

    [DebuggerDisplay("{" + nameof(DebugText) + "}")]
    public class XRefEntry
    {
        public long ObjectNumber { get; set; }

        public long Offset { get; set; }

        public long Revision { get; set; }

        public bool Free { get; set; }

        /// <summary>
        /// Determines if object is inside the object stream.
        /// </summary>
        public bool IsInStream { get; set; }

        /// <summary>
        /// If <see cref="IsInStream"/> then it denotes parent/stream object number.
        /// </summary>
        public long StreamObjectNumber { get; set; }

        public string DebugText => $"{Offset:D10} {Revision:D5} {(Free ? 'f' : 'n')} object {ObjectNumber} {Revision} {(IsInStream ? $"in stream {StreamObjectNumber}" : "")}";
    }
}