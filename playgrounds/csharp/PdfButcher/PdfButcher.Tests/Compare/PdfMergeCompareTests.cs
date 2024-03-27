namespace PdfButcher.Tests.Compare
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using PdfButcher.Tests.Common;

    /// <summary>
    /// Compares different implementations of PdfMergeService. Checks memory consumption, exec time and result file size.
    /// </summary>
    [TestFixture]
    // [Ignore("For development purposes")]
    public class PdfMergeCompareTests
    {
        private const int MaxInputSizeMegabytes = 500;

        private const bool DetailedTrace = false;

        private static readonly List<CompareCase> CompareCases = new List<CompareCase>
        {
            new CompareCase
            {
                CaseNumber = 200,
                ProviderName = nameof(TextSharp4PdfMergeService),
                Factory = () => new TextSharp4PdfMergeService(),
                Optimization = false,
            },
            new CompareCase
            {
                CaseNumber = 201,
                ProviderName = nameof(TextSharp4PdfMergeService),
                Factory = () => new TextSharp4PdfMergeService(),
                Optimization = true,
            },
            new CompareCase
            {
                CaseNumber = 202,
                ProviderName = nameof(ButcherPdfMergeService),
                Factory = () => new ButcherPdfMergeService(),
                Optimization = false,
            },
            new CompareCase
            {
                CaseNumber = 301,
                ProviderName = nameof(DynamicPdfMergeService),
                Factory = () => new DynamicPdfMergeService(),
                Optimization = false,
            },
        };

        private string _workingDir;
        private StreamWriter _reportWriter;
        private FileStream _reportStream;
        private MemoryWatcher _memoryWatcher;

        public static IEnumerable<TestCaseData> GetTestCases()
        {
            return CompareCases.Where(g => !g.Ignore).Select(c => new TestCaseData(c) { TestName = c.DisplayName, });
        }

        private static string StringifyOptions(object providerOptions)
        {
            if (providerOptions != null)
            {
                var properties = providerOptions.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

                var pairs = new List<KeyValuePair<string, string>>();

                foreach (var property in properties)
                {
                    var name = property.Name;
                    var value = property.GetValue(providerOptions);
                    if (value != null)
                    {
                        pairs.Add(new KeyValuePair<string, string>(name, value.ToString()));
                    }
                }

                var array = pairs.Select(g => $"{g.Key}={g.Value}").ToArray();
                if (array.Any())
                {
                    return string.Join(";", array);
                }
            }

            return string.Empty;
        }

        [OneTimeSetUp]
        public void ThisSetUp()
        {
            _workingDir = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(PdfMergeCompareTests));
            if (!Directory.Exists(_workingDir))
            {
                Directory.CreateDirectory(_workingDir);
            }

            var files = Directory.GetFiles(_workingDir, "*.*");
            foreach (var file in files)
            {
                File.Delete(file);
            }

            var reportFilePath = Path.Combine(_workingDir, "PdfMergeCompareResults.txt");
            if (File.Exists(reportFilePath))
            {
                File.Delete(reportFilePath);
            }

            _reportStream = File.Open(reportFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _reportWriter = new StreamWriter(_reportStream)
            {
                AutoFlush = true,
            };

            _memoryWatcher = new MemoryWatcher();
        }

        [OneTimeTearDown]
        public void ThisTearDown()
        {
            _reportWriter.Dispose();
            _reportStream.Dispose();
            _memoryWatcher.Dispose();
        }

        [Test]
        [TestCaseSource(typeof(PdfMergeCompareTests), nameof(GetTestCases))]
        public void Compare(CompareCase compareCase)
        {
            RunSingleCase(compareCase);
        }

        private void RunSingleCase(CompareCase compareCase)
        {
            var stopwatch = new Stopwatch();

            var outputFilePath = Path.Combine(_workingDir, $"Case_{compareCase.CaseNumber}_{compareCase.ProviderName}.pdf");

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            Log("################################################");
            Log($"Output file: {outputFilePath}");

            _memoryWatcher.CollectMemory();
            _memoryWatcher.Reset();

            stopwatch.Reset();
            stopwatch.Start();

            Trace($"    ...merging documents");

            var filesToMerge = GetEndlessFilesSource();

            long bytesRead = 0;
            long maxBytes = MaxInputSizeMegabytes * 1024 * 1024;
            int documentsWritten = 0;
            int pagesWritten = 0;

            using (var provider = compareCase.Factory())
            {
                provider.Initialize(compareCase.Optimization, outputFilePath);

                foreach (var fileToMerge in filesToMerge)
                {
                    var limitReached = bytesRead > maxBytes;

                    if (documentsWritten % 100 == 0 || documentsWritten == 0 || limitReached)
                    {
                        Trace($"    ...writing document {documentsWritten}");
                        Trace($"    ...current peak [MB]: {_memoryWatcher.PeakMegabytes}, current mem [MB]: {_memoryWatcher.LastMegabytes}");
                    }

                    if (limitReached)
                    {
                        break;
                    }

                    try
                    {
                        var numberOfPagesWritten = provider.MergeSinglePdf(fileToMerge);

                        documentsWritten++;

                        var loopFileInfo = new FileInfo(fileToMerge);
                        bytesRead += loopFileInfo.Length;

                        pagesWritten += numberOfPagesWritten;
                    }
                    catch (Exception exception)
                    {
                        Trace(fileToMerge);
                        Trace(exception.ToString());
                    }

                    _memoryWatcher.Update();
                }

                Trace($"    ...all documents written. Saving changes");
            }

            Trace($"    ...merging done");

            stopwatch.Stop();

            var fileInfo = new FileInfo(outputFilePath);

            Log($"Provider: {compareCase.ProviderName}");
            Log($"Case number: {compareCase.CaseNumber}");
            Log($"Description: {compareCase.Description}");
            Log($"Optimization: {compareCase.Optimization}");
            Log($"Provider options: {compareCase.OptionsString}");
            Log($"Pages written: {pagesWritten}");
            Log($"Documents written: {documentsWritten}");
            Log($"Input files size (sum) [MB]: {ToMegabytes(bytesRead)}");
            Log($"Memory true peak [MB]: {_memoryWatcher.FromBeginMegabytes}");
            Log($"Memory begin [MB]: {_memoryWatcher.BeginMegabytes}");
            Log($"Memory end [MB]: {_memoryWatcher.LastMegabytes}");
            Log($"Elapsed time [span]: {stopwatch.Elapsed}");
            Log($"Output file size [MB]: {ToMegabytes(fileInfo.Length)}");
        }

        private void Log(string message)
        {
            _reportWriter.WriteLine(message);
        }

        private void Trace(string message)
        {
            if (DetailedTrace)
            {
                _reportWriter.WriteLine(message);
            }
        }

        private IEnumerable<string> GetEndlessFilesSource()
        {
            var filesList = Directory.GetFiles(ResourcesHelper.BigFilesPath);

            while (true)
            {
                foreach (var file in filesList)
                {
                    yield return file;
                }
            }
        }

        private long ToMegabytes(long bytes)
        {
            return bytes / 1024 / 1024;
        }

#pragma warning disable CA1034
        public class CompareCase
        {
            public int CaseNumber { get; set; }

            public string Description { get; set; }

            public string ProviderName { get; set; }

            public Func<IPdfMergeService> Factory { get; set; }

            public bool Optimization { get; set; }

            public object ProviderOptions { get; set; }

            public string OptionsString => StringifyOptions(ProviderOptions);

            public string DisplayName => $"Case_{CaseNumber}_{ProviderName}_additional_optimization_{(Optimization ? "enabled" : "disabled")}";

            public bool Ignore { get; set; }
        }
#pragma warning restore CA1034
    }
}