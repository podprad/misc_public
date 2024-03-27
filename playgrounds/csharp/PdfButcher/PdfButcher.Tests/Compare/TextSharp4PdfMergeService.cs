namespace PdfButcher.Tests.Compare
{
    using System.IO;
    using iTextSharp.text;
    using iTextSharp.text.pdf;

    public class TextSharp4PdfMergeService : IPdfMergeService
    {
        private bool _optimize;
        private Document _document;
        private Stream _outputFileStream;
        private PdfCopy _writer;

        private int _currentFileIndex = 0;

        private static PdfCopy CreateWriter(Document document, Stream outputFileStream, bool optimization)
        {
            if (optimization)
            {
                return new PdfSmartCopy(document, outputFileStream);
            }
            else
            {
                return new PdfCopy(document, outputFileStream);
            }
        }

        public void Initialize(bool optimize, string outputFilePath)
        {
            var outputFileStream = File.OpenWrite(outputFilePath);
            Initialize(optimize, outputFileStream);
        }

        public void Initialize(bool optimize, Stream outputFileStream)
        {
            _optimize = optimize;
            _document = new Document();
            _outputFileStream = outputFileStream;
            _writer = CreateWriter(_document, _outputFileStream, _optimize);
        }

        public int MergeSinglePdf(string fileName)
        {
            return MergeSinglePdf(new PdfReader(fileName));
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                try
                {
                    _writer.Flush();
                    _writer.Close();
                }
                catch
                {
                    // Dispose() can throw exception when no pages were written.
                }
                finally
                {
                    _writer = null;
                }
            }

            if (_outputFileStream != null)
            {
                _outputFileStream.Dispose();
                _outputFileStream = null;
            }

            if (_document != null)
            {
                _document.Close();
                _document = null;
            }
        }

        private int MergeSinglePdf(PdfReader reader)
        {
            try
            {
                reader.ConsolidateNamedDestinations();

                if (_currentFileIndex == 0)
                {
                    _document.SetPageSize(reader.GetPageSizeWithRotation(1));

                    if (!_document.IsOpen())
                    {
                        _document.Open();
                    }
                }

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    var importedPage = _writer.GetImportedPage(reader, i);
                    _writer.AddPage(importedPage);
                }

                _currentFileIndex++;

                return reader.NumberOfPages;
            }
            finally
            {
                _writer.FreeReader(reader);
                _writer.Flush();
                reader.Close();
            }
        }
    }
}