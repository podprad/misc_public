namespace PdfButcher.Tests.Internals
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals.IO;
    using PdfButcher.Tests.Common;

    /// <summary>
    /// Checks if all included resources can be read and write.
    /// </summary>
    [TestFixture]
    public class ResourcesReadWriteTests
    {
        private string _resultDir;

        [OneTimeSetUp]
        public void SetUp()
        {
            _resultDir = Path.Combine(TestContext.CurrentContext.TestDirectory, nameof(ResourcesReadWriteTests));
            if (!Directory.Exists(_resultDir))
            {
                Directory.CreateDirectory(_resultDir);
            }
        }

        [Test]
        [TestCaseSource(typeof(ResourcesReadWriteTests), nameof(GetTestCases))]
        public void Should_open_and_save(string fileName, string newFileNameWithoutExtension)
        {
            var originalFilePath = Path.Combine(_resultDir, newFileNameWithoutExtension + "_original.pdf");
            var modifiedFilePath = Path.Combine(_resultDir, newFileNameWithoutExtension + "_modified.pdf");

            if (!File.Exists(originalFilePath))
            {
                File.Copy(fileName, originalFilePath);
            }

            if (File.Exists(modifiedFilePath))
            {
                File.Delete(modifiedFilePath);
            }

            try
            {
                using (var pdfMerge = new PdfMerge(modifiedFilePath))
                {
                    pdfMerge.AddDocument(originalFilePath);
                }

                using (var originalDocument = PdfReader.ReadDocument(originalFilePath))
                {
                    using (var modifiedDocument = PdfReader.ReadDocument(modifiedFilePath))
                    {
                        TestHelper.DeepCompare(originalDocument, modifiedDocument);
                    }
                }
            }
            catch
            {
                File.Delete(modifiedFilePath);

                throw;
            }
        }

        private static IEnumerable<TestCaseData> GetTestCases()
        {
            var files = Directory.GetFiles(ResourcesHelper.GetPdfResourcesPath(), "*.pdf", SearchOption.AllDirectories);

            return files.Select(originalFileName =>
            {
                var directoryInfo = Directory.GetParent(originalFileName);
                var parentDirectoryName = directoryInfo.Name;

                var originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                var newFileNameWithoutExtension = parentDirectoryName + "_" + originalFileNameWithoutExtension + "_";

                return new TestCaseData(originalFileName, newFileNameWithoutExtension)
                {
                    TestName = newFileNameWithoutExtension
                };
            }).ToList();
        }
    }
}