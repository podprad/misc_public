namespace PdfButcher.Internals.Model
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class PdfArray : PdfObject
    {
        private readonly List<PdfObject> _list = new List<PdfObject>();

        public PdfArray()
        {
        }

        public PdfArray(IEnumerable<PdfObject> pdfObjects)
        {
            AddRange(pdfObjects);
        }

        public IEnumerable<PdfObject> Values => _list;

        public int Count => _list.Count;

        public void Add(PdfObject pdfObject)
        {
            _list.Add(pdfObject);
        }

        public void AddRange(IEnumerable<PdfObject> pdfObjects)
        {
            _list.AddRange(pdfObjects);
        }

        public override PdfObject Clone(IPdfIndirectObjectsResolver resolver)
        {
            var clonedItems = _list.Select(pdfObject => pdfObject.Clone(resolver));
            var result = new PdfArray(clonedItems);

            return result;
        }

        public override void WriteTo(Stream stream)
        {
            stream.WriteByte(PdfConstants.ArrayStartByte);
            stream.WriteByte(PdfConstants.WhiteSpaceByte);

            for (var index = 0; index < _list.Count; index++)
            {
                var child = _list[index];
                child.WriteTo(stream);

                if (index != _list.Count - 1)
                {
                    stream.WriteByte(PdfConstants.WhiteSpaceByte);
                }
            }

            stream.WriteByte(PdfConstants.WhiteSpaceByte);
            stream.WriteByte(PdfConstants.ArrayEndByte);
        }
    }
}