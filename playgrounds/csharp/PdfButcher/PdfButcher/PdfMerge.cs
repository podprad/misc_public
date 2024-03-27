namespace PdfButcher
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.IO;

    public class PdfMerge : IDisposable
    {
        private readonly Stream _outputStream;
        private readonly bool _leaveOpen;
        private readonly PdfWriter _writer;
        private int _documentsWritten = 0;

        public PdfMerge(string outputFileName)
          : this(File.Open(outputFileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read), false)
        {
        }

        public PdfMerge(Stream outputStream, bool leaveOpen = true)
        {
            _outputStream = outputStream;
            _leaveOpen = leaveOpen;
            _writer = new PdfWriter(_outputStream);
        }

        public int AddDocument(string fileName)
        {
            return AddDocument(File.OpenRead(fileName), false);
        }

        public int AddDocument(Stream inputStream, bool leaveOpen = true)
        {
            using (var inputDocument = PdfReader.ReadDocument(inputStream, leaveOpen))
            {
                if (_documentsWritten == 0)
                {
                    _writer.SetVersion(inputDocument.Version);
                    _writer.WriteVersionHeader();
                }

                _documentsWritten++;

                var pagesCount = inputDocument.PagesCount;
                _writer.WriteObjects(inputDocument.GetIndirectObjects().Select(pdfObject => pdfObject.Clone(inputDocument)));

                return (int)pagesCount;
            }
        }

        public void Dispose()
        {
            _writer.WriteEnd();

            if (!_leaveOpen)
            {
                _outputStream.Dispose();
            }
        }
    }
}