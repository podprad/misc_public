namespace PdfButcher.Tests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals;
    using PdfButcher.Internals.Model;

    internal static class TestHelper
    {
        public static TreeItem BuildTree(IPdfDocument a)
        {
            var stack = new Stack<TreeItem>();
            var walked = new HashSet<object>();

            var root = TreeItem.Create(null, a);
            var catalog = TreeItem.Create(root, a.Catalog);

            stack.Push(catalog);

            while (true)
            {
                if (!stack.Any())
                {
                    break;
                }

                var objectA = stack.Pop();

                if (walked.Contains(objectA.Value))
                {
                    continue;
                }

                walked.Add(objectA.Value);

                if (objectA.Value is PdfIndirectObject pdfIndirectObject)
                {
                    stack.Push(TreeItem.Create(objectA, pdfIndirectObject.PdfObject));

                    if (pdfIndirectObject.PdfStream != null)
                    {
                        TreeItem.Create(objectA, pdfIndirectObject.PdfStream);
                    }
                }
                else if (objectA.Value is PdfDictionary pdfDictionary)
                {
                    var sortedKeys = pdfDictionary.Keys.Where(g => g.Value != PdfNames.Parent && g.Value != PdfNames.ParentTree).OrderBy(g => g.Value).ToList();

                    foreach (var key in sortedKeys)
                    {
                        var nameParent = TreeItem.Create(objectA, key);
                        var valueA = pdfDictionary.GetOrThrow(key).ResolveValueOrThrow(a);

                        var valueItem = TreeItem.Create(nameParent, valueA);
                        stack.Push(valueItem);
                    }
                }
                else if (objectA.Value is PdfArray pdfArray)
                {
                    foreach (var value in pdfArray.Values)
                    {
                        stack.Push(TreeItem.Create(objectA, value));
                    }
                }
                else if (objectA.Value is PdfReference pdfReference)
                {
                    var resolvedA = a.GetIndirectObjectOrNull(pdfReference.ReferencedObjectId);

                    stack.Push(TreeItem.Create(objectA, resolvedA));
                }
                else if (objectA.Value is PdfObject pdfObject)
                {
                    TreeItem.Create(objectA, pdfObject);
                }
                else if (objectA.Value == null)
                {
                }
                else
                {
                    throw new InvalidOperationException("Unknown type");
                }
            }

            return root;
        }

        public static string DumpPath(TreeItem item)
        {
            var pathToRoot = new List<TreeItem>();

            var stack = new Stack<TreeItem>();
            stack.Push(item);

            while (stack.Any())
            {
                var current = stack.Pop();
                pathToRoot.Insert(0, current);

                if (current.Parent != null)
                {
                    stack.Push(current.Parent);
                }
            }

            var result = string.Join("\r\n--> ", pathToRoot.Select(g => g.Value.ToString()));
            return result;
        }

        public static void DeepCompare(TreeItem a, TreeItem b)
        {
            var aLeafs = a.GetLeafs().ToList();
            var bLeafs = b.GetLeafs().ToList();

            for (var index = 0; index < aLeafs.Count; index++)
            {
                var aLeaf = aLeafs[index];
                var bLeaf = bLeafs[index];

                if (aLeaf.Value is PdfObject pdfObjectA)
                {
                    var pdfObjectB = bLeaf.Value as PdfObject;

                    if (pdfObjectB == null || pdfObjectA.GetType() != pdfObjectB.GetType() || pdfObjectA.ToString() != pdfObjectB.ToString())
                    {
                        var leftDump = DumpPath(aLeaf);
                        var rightDump = DumpPath(bLeaf);

                        var builder = new StringBuilder();
                        builder.AppendLine("Different objects");
                        builder.AppendLine("Object A:");
                        builder.AppendLine(leftDump);
                        builder.AppendLine("Object B:");
                        builder.AppendLine(rightDump);
                        Assert.Fail(builder.ToString());
                    }
                }
                else if (aLeaf.Value is PdfStream pdfStreamA)
                {
                    var pdfStreamB = (PdfStream)bLeaf.Value;
                    var indirectA = (PdfIndirectObject)aLeaf.Parent.Value;
                    var indirectB = (PdfIndirectObject)bLeaf.Parent.Value;

                    var ownerA = indirectA.PdfObject.AsOrThrow<PdfDictionary>();
                    var ownerB = indirectB.PdfObject.AsOrThrow<PdfDictionary>();

                    var bufferA = new MemoryStream();
                    var bufferB = new MemoryStream();

                    pdfStreamA.CopyEncodedStream(ownerA, (IPdfDocument)a.Value, bufferA);
                    pdfStreamB.CopyEncodedStream(ownerB, (IPdfDocument)b.Value, bufferB);

                    var arrayA = bufferA.ToArray();
                    var arrayB = bufferB.ToArray();

                    arrayA.Should().BeEquivalentTo(arrayB);
                }
            }
        }

        public static void DeepCompare(IPdfDocument a, IPdfDocument b)
        {
            a.PagesCount.Should().Be(b.PagesCount);
            a.Version.Should().Be(b.Version);

            var aTree = BuildTree(a);
            var bTree = BuildTree(b);

            DeepCompare(aTree, bTree);
        }

        public static string GetContent(Stream stream, int length = -1)
        {
            using (var reader = new StreamReader(stream, PdfConstants.PdfFileEncoding, false, 1024, true))
            {
                if (length >= 0)
                {
                    var buffer = new char[length];
                    reader.Read(buffer, 0, length);

                    return new string(buffer);
                }

                return reader.ReadToEnd();
            }
        }

        public static byte[] HexStringToBytes(string hexString)
        {
            var listOfBytes = new List<byte>();

            var chars = hexString.Replace(" ", "").ToCharArray();

            for (int i = 0; i < chars.Length; i += 2)
            {
                var c1 = chars[i];
                var c2 = chars[i + 1];

                var word = c1 + "" + c2;
                var b = Convert.ToByte(word, 16);
                listOfBytes.Add(b);
            }

            return listOfBytes.ToArray();
        }

        public static void DumpAndOpen(Stream stream, string fileName = "dump.pdf")
        {
            var oldPosition = stream.Position;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.Position = 0;
                    stream.CopyTo(memoryStream);

                    var bytes = memoryStream.ToArray();

                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }

                    File.WriteAllBytes(fileName, bytes);

                    Process.Start(fileName);
                }
            }
            finally
            {
                stream.Position = oldPosition;
            }
        }
    }
}