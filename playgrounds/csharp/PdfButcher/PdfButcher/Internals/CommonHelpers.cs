namespace PdfButcher.Internals
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.Model;

    internal static class CommonHelpers
    {
        public static void CopyStream(Stream source, Stream destination, long length, int bufferSize = 16 * 1024)
        {
            var bytesLeft = length;

            using (var buffer = new RentArray<byte>(bufferSize))
            {
                while (true)
                {
                    var bytesToRead = (int)Math.Min(buffer.Length, bytesLeft);

                    var readCount = source.Read(buffer.Array, 0, bytesToRead);
                    if (readCount == 0)
                    {
                        break;
                    }

                    destination.Write(buffer.Array, 0, readCount);

                    bytesLeft -= readCount;
                }
            }
        }

        public static bool IsWhiteSpace(byte b)
        {
            return b == 0x00 // \0
                   || b == 0x09 // \t
                   || b == 0x0A // \n
                   || b == 0x0C // \f
                   || b == 0x0D // \r
                   || b == 0x20; // ' '
        }

        public static bool IsDigit(int b)
        {
            return b >= '0' && b <= '9';
        }

        public static bool IsOctDigit(int b)
        {
            return b >= '0' && b <= '7';
        }

        public static bool IsHexDigit(int b)
        {
            return (b >= '0' && b <= '9')
                   || (b >= 'A' && b <= 'F')
                   || (b >= 'a' && b <= 'f');
        }

        public static bool IsLetter(int b)
        {
            return (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z');
        }

        public static bool TryFillArray(this Stream stream, byte[] array, int requiredBytes = -1, int offset = 0)
        {
            if (requiredBytes < 0)
            {
                requiredBytes = array.Length - offset;
            }

            var total = offset;
            int read;
            while ((read = stream.Read(array, total, (requiredBytes - total) + offset)) > 0)
            {
                total += read;
            }

            return total == requiredBytes;
        }

        public static IEnumerable<PdfObject> Flatten(this PdfObject pdfObject, bool includeSelf = false, Func<PdfDictionary, PdfName, PdfObject, bool> shouldBeIgnored = null)
        {
            var stack = new Stack<PdfObject>();

            stack.Push(pdfObject);

            while (stack.Any())
            {
                var current = stack.Pop();

                if (current == pdfObject)
                {
                    if (includeSelf)
                    {
                        yield return current;
                    }
                }
                else
                {
                    yield return current;
                }

                if (current is PdfDictionary pdfDictionary)
                {
                    foreach (var key in pdfDictionary.Keys)
                    {
                        var value = pdfDictionary.GetOrThrow(key);
                        if (shouldBeIgnored != null && shouldBeIgnored(pdfDictionary, key, value))
                        {
                            continue;
                        }

                        stack.Push(value);
                    }
                }
                else if (current is PdfArray pdfArray)
                {
                    foreach (var item in pdfArray.Values)
                    {
                        stack.Push(item);
                    }
                }
            }
        }

        /// <summary>
        /// Gets page-specific dependencies, recursively.
        /// Parent ignored.
        /// </summary>
        public static IEnumerable<PdfIndirectObject> GetPageDescendants(IPdfDocument pdfDocument, PdfIndirectObject pdfPage)
        {
            var alreadyProcessed = new HashSet<PdfIndirectObject>();

            var stack = new Stack<PdfIndirectObject>();

            stack.Push(pdfPage);

            while (stack.Any())
            {
                var current = stack.Pop();

                if (alreadyProcessed.Contains(current))
                {
                    continue;
                }

                alreadyProcessed.Add(current);

                // /Type /Page /Parent - will make recursive scan. We will go to parent and again to page.
                // Avoid it.
                var resolvedChildren = current.PdfObject.Flatten(false, (_, key, value) => key.Equals(PdfNames.Parent))
                    .OfType<PdfReference>()
                    .Select(g => pdfDocument.GetIndirectObjectOrThrow(g.ReferencedObjectId));

                foreach (var resolvedChild in resolvedChildren)
                {
                    yield return resolvedChild;

                    stack.Push(resolvedChild);
                }
            }
        }
    }
}