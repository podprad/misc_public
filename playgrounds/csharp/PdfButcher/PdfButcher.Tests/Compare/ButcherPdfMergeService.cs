namespace PdfButcher.Tests.Compare
{
    using System.IO;

    public class ButcherPdfMergeService : IPdfMergeService
    {
        private PdfMerge _merge;

        public void Dispose()
        {
            _merge.Dispose();
        }

        public void Initialize(bool optimize, string outputFilePath)
        {
            _merge = new PdfMerge(File.OpenWrite(outputFilePath), false);
        }

        public int MergeSinglePdf(string fileName)
        {
            return _merge.AddDocument(File.OpenRead(fileName), false);
        }
    }
}