namespace PdfButcher.Benchmarks.StreamReading
{
    using System;
    using System.IO;
    using BenchmarkDotNet.Attributes;
    using PdfButcher.Benchmarks.StreamReading.LookAheadReaders;
    using static Sizes;

#pragma warning disable CA1001

    /// <summary>
    /// What is optimal way of reading bytes from the stream? What is the optimal buffer size?
    /// </summary>
    public class ByteReadingBenchmark
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

            var testFile = TestFile.ZeroFilled2MB;
            _largeFilePath = _testFiles.GetFileName(testFile);
            _largeFileBytes = _testFiles.GetBytes(testFile);
        }

        // Slow as hell
        // [Benchmark]
        [Arguments(StreamSource.File)]
        public void Stream_ReadByte(StreamSource streamSource)
        {
            using (var stream = GetStream(streamSource))
            {
                while (true)
                {
                    var readResult = stream.ReadByte();
                    if (readResult < 0)
                    {
                        break;
                    }

                    DoNothing((byte)readResult);
                }
            }
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
        public void StaticByteReader_ReadByte(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                StaticByteReader.InitRead(out var bufferPosition, out var bufferFill, out var buffer, bufferSize);

                while (true)
                {
                    var readResult = StaticByteReader.ReadByte(stream, ref bufferPosition, ref bufferFill, buffer);
                    if (readResult < 0)
                    {
                        break;
                    }

                    DoNothing((byte)readResult);
                }
            }
        }

        // Slower than static one, why?
        [Benchmark]
        [Arguments(StreamSource.File, OptimalBufferSize)]
        public void NonStaticByteReader_ReadByte(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                var reader = new NonStaticByteReader(stream, bufferSize);

                while (true)
                {
                    var readResult = reader.ReadByte();
                    if (readResult < 0)
                    {
                        break;
                    }

                    DoNothing((byte)readResult);
                }
            }
        }

        // [Benchmark]
        [Arguments(StreamSource.File, OptimalBufferSize)]
        public void StaticByteReader_InlineCopiedCode(StreamSource streamSource, int bufferSize)
        {
            using (var stream = GetStream(streamSource))
            {
                var bufferPosition = -1;
                var bufferFill = -1;
                var buffer = new byte[bufferSize];

                while (true)
                {
                    if (bufferPosition > bufferFill - 1 || bufferPosition < 0)
                    {
                        bufferFill = stream.Read(buffer, 0, buffer.Length);
                        bufferPosition = 0;

                        if (bufferFill == 0)
                        {
                            break;
                        }
                    }

                    var readResult = buffer[bufferPosition++];

                    DoNothing(readResult);
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