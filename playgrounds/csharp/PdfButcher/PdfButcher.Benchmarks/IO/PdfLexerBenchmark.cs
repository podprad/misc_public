namespace PdfButcher.Benchmarks.IO
{
    using System;
    using System.IO;
    using BenchmarkDotNet.Attributes;
    using PdfButcher.Internals;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.IO.LookAhead;
    using PdfButcher.Internals.Model;

    public class PdfLexerBenchmark
    {
        private TestFiles _testFiles;
        private string _testFilePath;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _testFiles = new TestFiles();
            _testFilePath = _testFiles.GetFileName(TestFile.LexStream2MB);
        }

        [Benchmark]
        [Arguments(nameof(BinaryLookAheadBuffer))]
        [Arguments(nameof(StreamLookAheadReader))]
        public void Lex_without_buffer(string lookAheadReaderName)
        {
            using (var fileStream = File.OpenRead(_testFilePath))
            {
                DoLex(fileStream, lookAheadReaderName);
            }
        }

        [Benchmark]
        [Arguments(nameof(BinaryLookAheadBuffer))]
        [Arguments(nameof(StreamLookAheadReader))]
        public void Lex_with_buffer(string lookAheadReaderName)
        {
            using (var fileStream = File.OpenRead(_testFilePath))
            {
                using (var bufferedStream = new BufferedStream(fileStream, PdfConstants.OptimalBufferSize))
                {
                    DoLex(bufferedStream, lookAheadReaderName);
                }
            }
        }

        private ILookAheadReader<int> CreateLookAheadReader(string readerTypeName, Stream stream)
        {
            if (readerTypeName == nameof(StreamLookAheadReader))
            {
                return new StreamLookAheadReader(stream);
            }

            if (readerTypeName == nameof(BinaryLookAheadBuffer))
            {
                return new BinaryLookAheadBuffer(stream);
            }

            throw new NotSupportedException(readerTypeName);
        }

        private void DoLex(Stream stream, string lookAheadReaderName)
        {
            var lexer = new PdfLexer(stream, s => CreateLookAheadReader(lookAheadReaderName, s));

            while (true)
            {
                var t = lexer.Next();
                if (t.TokenType == PdfTokenType.EndOfFile)
                {
                    break;
                }
            }
        }
    }
}