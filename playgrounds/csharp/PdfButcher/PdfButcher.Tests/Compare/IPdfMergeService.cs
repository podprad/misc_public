namespace PdfButcher.Tests.Compare
{
    using System;

    public interface IPdfMergeService : IDisposable
    {
        void Initialize(bool optimize, string outputFilePath);

        int MergeSinglePdf(string fileName);
    }
}