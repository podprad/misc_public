namespace PdfButcher.Tests.Compare
{
    using System.IO;
    using ceTe.DynamicPDF;
    using ceTe.DynamicPDF.Merger;

    public class DynamicPdfMergeService : IPdfMergeService
    {
        private MergeDocument _merge;
        private string _outputFilePath;

        public void Dispose()
        {
            _merge.Draw(_outputFilePath);
        }

        public void Initialize(bool optimize, string outputFilePath)
        {
            _outputFilePath = outputFilePath;
            _merge = new MergeDocument
            {
                DiskBuffering = new DiskBufferingOptions
                {
                    Enabled = true,
                    Location = Path.GetDirectoryName(outputFilePath),
                },
            };
        }

        public int MergeSinglePdf(string fileName)
        {
            var appendedPages = _merge.Append(fileName);

            return appendedPages.Length;
        }
    }
}