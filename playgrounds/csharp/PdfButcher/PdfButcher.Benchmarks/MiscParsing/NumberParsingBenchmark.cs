namespace PdfButcher.Benchmarks.MiscParsing
{
    using System.Globalization;
    using System.Linq;
    using BenchmarkDotNet.Attributes;
    using PdfButcher.Internals;
    using PdfButcher.Internals.IO;

    public class NumberParsingBenchmark
    {
        private static readonly byte[] LongChars = new[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', }.Select(c => (byte)c).ToArray();

        [Benchmark]
        public void EncodingGetStringAndParse()
        {
            var stringValue = PdfConstants.PdfFileEncoding.GetString(LongChars);
            var longValue = long.Parse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        [Benchmark]
        public void HandCrafted()
        {
            var parsedNumber = EncodingHelper.DecodeNumber(LongChars, LongChars.Length);
        }
    }
}