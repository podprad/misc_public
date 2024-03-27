namespace PdfButcher.Benchmarks.StreamReading
{
    using System;
    using System.IO;
    using BenchmarkDotNet.Attributes;
    using PdfButcher.Benchmarks.StreamReading.LookAheadReaders;
    using PdfButcher.Internals.IO.LookAhead;
    using static Sizes;

#pragma warning disable CA1001

    public class LoohAheadReadingBenchmark
    {
        private const int OptimalBufferSize = Kilobyte * 32;

        private TestFiles _testFiles;
        private string _largeFilePath;
        private byte[] _largeFileBytes;

#pragma warning disable CA1801
        private static void DoNothing(byte b)
        {
        }
#pragma warning restore CA1801

        [GlobalSetup]
        public void GlobalSetup()
        {
            _testFiles = new TestFiles();

            var testFile = TestFile.LexStream2MB;
            _largeFilePath = _testFiles.GetFileName(testFile);
            _largeFileBytes = _testFiles.GetBytes(testFile);
        }

        /// <summary>
        /// Baseline calling Stream.Read without any overhead.
        /// </summary>
        [Benchmark]
        [Arguments(StreamSource.File, Kilobyte * 32)]
        public void StreamArrayReading(StreamSource streamSource, int bufferSize)
        {
            var buffer = new byte[bufferSize];

            using (var stream = GetStream(streamSource))
            {
                while (true)
                {
                    var readCount = stream.Read(buffer, 0, buffer.Length);
                    if (readCount == 0)
                    {
                        break;
                    }

                    for (int i = 0; i < readCount; i++)
                    {
                        var readByte = buffer[i];
                        DoNothing(readByte);
                    }
                }
            }
        }

        [Benchmark]
        [Arguments(StreamSource.File, OptimalBufferSize)]
        public void BufferedStreamReading(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                using (var bufferedStream = new BufferedStream(stream, bufferSize))
                {
                    while (true)
                    {
                        var readByte = bufferedStream.ReadByte();
                        if (readByte < 0)
                        {
                            break;
                        }

                        DoNothing((byte)readByte);
                    }
                }
            }
        }

        [Benchmark]
        [Arguments(StreamSource.File)]
        public void RawStreamLookAheadReader(StreamSource streamSource)
        {
            using (var stream = GetStream(streamSource))
            {
                var reader = new RawStreamLookAheadReader(stream);

                DoLookAhead(reader);
            }
        }

        [Benchmark]
        [Arguments(StreamSource.File, OptimalBufferSize)]
        public void BufferedStreamLookAheadReader(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                var reader = new BufferedStreamLookAheadReader(stream, bufferSize);
                DoLookAhead(reader);
            }
        }

        [Benchmark]
        [Arguments(StreamSource.File, OptimalBufferSize)]
        public void CustomBufferingLookAheadReader(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                var reader = new CustomBufferingLookAheadReader(stream, bufferSize);

                DoLookAhead(reader);
            }
        }

        [Benchmark]
        [Arguments(StreamSource.File)]
        public void BinaryLookAheadReader(StreamSource streamSource)
        {
            using (var stream = GetStream(streamSource))
            {
                var reader = new BinaryLookAheadBuffer(stream);
                DoLookAhead(reader);
            }
        }

        private void DoLookAhead(ILookAheadReader<int> reader)
        {
            while (true)
            {
                var readByte = reader.Read();
                if (readByte < 0)
                {
                    break;
                }

                var ahead = reader.LookAhead();
                if (ahead < 0)
                {
                    break;
                }

                if (readByte == '#')
                {
                    reader.Clear();
                }

                DoNothing((byte)readByte);
            }
        }

        private Stream GetStream(StreamSource streamSource)
        {
            if (streamSource == StreamSource.File)
            {
                return File.OpenRead(_largeFilePath);
            }

            if (streamSource == StreamSource.Memory)
            {
                return new MemoryStream(_largeFileBytes);
            }

            throw new InvalidOperationException($"Unknown stream source {streamSource}");
        }
    }
}