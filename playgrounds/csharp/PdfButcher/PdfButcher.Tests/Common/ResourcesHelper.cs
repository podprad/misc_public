namespace PdfButcher.Tests.Common
{
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    public static class ResourcesHelper
    {
        public const string BigFilesPath = @"";

        public static Stream OpenRead(ResourceFile resourceFile)
        {
            return File.OpenRead(GetCustomPdfResourcePath(resourceFile));
        }

        public static string GetCustomPdfResourcePath(ResourceFile resourceFile)
        {
            return GetCustomPdfResourcePath($"{resourceFile}.pdf");
        }

        public static string GetCustomPdfResourcePath(string fileName)
        {
            return Path.Combine(GetPdfResourcesPath(), $"Custom\\{fileName}");
        }

        public static string GetPdfResourcesPath()
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, $"Resources\\Pdfs");
        }

        public static IEnumerable<string> GetAllPdfResources()
        {
            return Directory.GetFiles(GetPdfResourcesPath(), "*.pdf", SearchOption.AllDirectories);
        }
    }
}