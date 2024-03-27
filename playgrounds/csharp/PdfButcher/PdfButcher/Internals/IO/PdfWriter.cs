namespace PdfButcher.Internals.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.Model;

    public class PdfWriter
    {
        private readonly Stream _stream;
        private readonly PdfWriterDocument _document;

        public PdfWriter(Stream stream)
        {
            _stream = stream;
            _document = new PdfWriterDocument();
        }

        internal bool WriteCommentWithObjectOffset { get; set; } = false;

        internal bool WriteCommentWithProductInfoAndDate { get; set; } = false;

        public void SetVersion(int pdfVersion)
        {
            _document.Version = pdfVersion;
        }

        public void WriteVersionHeader()
        {
            EncodingHelper.RawWrite(_stream, "%PDF-");

            var versionString = _document.Version.ToString(CultureInfo.InvariantCulture);
            if (versionString.Length >= 2)
            {
                EncodingHelper.RawWrite(_stream, versionString[0]);
                EncodingHelper.RawWrite(_stream, ".");
                EncodingHelper.RawWrite(_stream, versionString[1] + "\r\n");
            }
            else
            {
                EncodingHelper.RawWrite(_stream, "1.4\r\n");
            }

            _stream.Flush();
        }

        public void WriteObjects(IEnumerable<PdfIndirectObject> pdfIndirectObjects)
        {
            AddObjects(pdfIndirectObjects);
            WriteUnwrittenObjects();
        }

        public void WriteEnd()
        {
            WriteUnwrittenObjects();

            _document.UpdatePagesDictionary();

            var pagesPosition = WriteIndirectObject(_document.Pages);
            _document.MarkAsWritten(_document.Pages.Id, pagesPosition);

            var catalogPosition = WriteIndirectObject(_document.Catalog);
            _document.MarkAsWritten(_document.Catalog.Id, catalogPosition);

            _document.UpdateTrailer();

            WriteXRefTableAndTrailer();
            WriteEofComment();
        }

        private void AddObjects(IEnumerable<PdfIndirectObject> pdfObjects)
        {
            _document.AddObjects(pdfObjects);
        }

        private void WriteUnwrittenObjects()
        {
            foreach (var unwrittenObject in _document.GetUnwrittenObjects())
            {
                var writePosition = WriteIndirectObject(unwrittenObject);
                _document.MarkAsWritten(unwrittenObject.Id, writePosition);
            }
        }

        private long WriteIndirectObject(PdfIndirectObject pdfObject)
        {
            if (pdfObject.Removed)
            {
                return -1;
            }

            var startPosition = _stream.Position;

            if (WriteCommentWithObjectOffset)
            {
                EncodingHelper.RawWriteLine(_stream, $"%{startPosition}");
            }

            EncodingHelper.RawWrite(_stream, pdfObject.Id.Id.ToString(CultureInfo.InvariantCulture));
            EncodingHelper.RawWrite(_stream, " ");
            EncodingHelper.RawWrite(_stream, pdfObject.Id.Revision.ToString(CultureInfo.InvariantCulture));
            EncodingHelper.RawWrite(_stream, " ");
            EncodingHelper.RawWriteLine(_stream, PdfKeywords.Obj);

            EncodingHelper.RawWriteLine(_stream, pdfObject.PdfObject.ToString());

            if (pdfObject.PdfObject is PdfDictionary pdfDictionary)
            {
                if (pdfObject.PdfStream != null)
                {
                    var pdfStream = pdfObject.PdfStream;

                    EncodingHelper.RawWriteLine(_stream, PdfKeywords.Stream);
                    _stream.Flush();
                    pdfStream.CopyEncodedStream(pdfDictionary, _document, _stream);
                    EncodingHelper.RawWriteLine(_stream);
                    EncodingHelper.RawWriteLine(_stream, PdfKeywords.EndStream);
                }
            }

            EncodingHelper.RawWriteLine(_stream, PdfKeywords.EndObj);

            _stream.Flush();

            return startPosition;
        }

        private void WriteXRefTableAndTrailer()
        {
            var xRefStartPosition = _stream.Position;

            EncodingHelper.RawWriteLine(_stream, PdfKeywords.XRef);

            var groups = _document.XRefTable.CreateWriteGroups().ToList();

            foreach (var group in groups)
            {
                for (var index = 0; index < group.Count; index++)
                {
                    var entry = group[index];
                    if (index == 0)
                    {
                        EncodingHelper.RawWriteLine(_stream, $"{entry.ObjectNumber} {group.Count}");
                    }

                    EncodingHelper.RawWrite(_stream, entry.Offset.ToString("0000000000"));
                    EncodingHelper.RawWrite(_stream, " ");
                    EncodingHelper.RawWrite(_stream, entry.Revision.ToString("00000"));
                    EncodingHelper.RawWrite(_stream, " ");
                    EncodingHelper.RawWrite(_stream, entry.Free ? PdfKeywords.F : PdfKeywords.N);
                    EncodingHelper.RawWriteLine(_stream);
                }
            }

            EncodingHelper.RawWriteLine(_stream, PdfKeywords.Trailer);
            EncodingHelper.RawWriteLine(_stream, _document.Trailer.ToString());

            EncodingHelper.RawWriteLine(_stream, PdfKeywords.StartXRef);
            EncodingHelper.RawWriteLine(_stream, xRefStartPosition.ToString(CultureInfo.InvariantCulture));
            _stream.Flush();
        }

        private void WriteEofComment()
        {
            if (WriteCommentWithProductInfoAndDate)
            {
                EncodingHelper.RawWriteLine(_stream, $"% Created by {PdfConstants.ProductName} {PdfConstants.ProductVersion}");
                EncodingHelper.RawWriteLine(_stream, $"% Creation date {DateTime.Now:O}");
            }

            // No new line after %%EOF
            EncodingHelper.RawWrite(_stream, "%%EOF");
            _stream.Flush();
        }
    }
}