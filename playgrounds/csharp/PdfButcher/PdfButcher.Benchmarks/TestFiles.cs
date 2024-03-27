namespace PdfButcher.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using PdfButcher.Internals;

    internal class TestFiles
    {
        private readonly Dictionary<TestFile, Action<Stream>> _factories = new Dictionary<TestFile, Action<Stream>>
        {
            {
                TestFile.ZeroFilled2MB, stream =>
                {
                    for (int i = 0; i < Sizes.Megabyte * 2; i++)
                    {
                        stream.WriteByte(0);
                    }
                }
            },
            {
                TestFile.Containing2048BytesOfABCDAfter1MB, stream =>
                {
                    for (int i = 0; i < Sizes.Megabyte; i++)
                    {
                        stream.WriteByte(0);
                    }

                    var text = CreatePattern("ABCD", 2048);
                    stream.Write(text, 0, text.Length);
                }
            },
            {
                TestFile.LexStream2MB, stream =>
                {
                    const string Text = "1 0 obj\r\n<< /Type /Pages /Kids [1 0 R 2 0 R 3 0 R ] /TestStr (Hello World) /TestInt 12345 /TestReal 123.456 /TestBool1 true /TestBool2 false /TestNull null >>\r\nendobj#";
                    var bytesLeft = Sizes.Megabyte * 2;

                    var bytes = PdfConstants.PdfFileEncoding.GetBytes(Text);

                    while (true)
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        bytesLeft -= bytes.Length;

                        if (bytesLeft <= 0)
                        {
                            break;
                        }
                    }
                }
            },
        };

        public static byte[] CreatePattern(string text, int expandToBytes)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            var repeatCount = expandToBytes / bytes.Length;

            IEnumerable<byte> result = bytes;

            for (int i = 1; i < repeatCount; i++)
            {
                result = result.Concat(bytes);
            }

            var resultBytes = result.ToArray();

            return resultBytes;
        }

        private static string GetPath(string caseName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", $"{caseName}.txt");

            return path;
        }

        private static string GenerateCase(TestFile testFile, Action<Stream> generator)
        {
            var path = GetPath(testFile.ToString());

            if (File.Exists(path))
            {
                return path;
            }

            var directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            using (var stream = File.OpenWrite(path))
            {
                generator(stream);
            }

            return path;
        }

        public IEnumerable<TestFile> GetAllCases()
        {
            return Enum.GetValues(typeof(TestFile)).OfType<TestFile>();
        }

        public FileStream OpenFileStream(TestFile testFile)
        {
            return File.OpenRead(this.GetFileName(testFile));
        }

        public byte[] GetBytes(TestFile testFile)
        {
            return File.ReadAllBytes(this.GetFileName(testFile));
        }

        public string GetFileName(TestFile testFile)
        {
            var func = this._factories[testFile];
            var path = GenerateCase(testFile, func);

            return path;
        }

        public void GenerateAll()
        {
            foreach (var pair in _factories)
            {
                GenerateCase(pair.Key, pair.Value);
            }
        }
    }
}