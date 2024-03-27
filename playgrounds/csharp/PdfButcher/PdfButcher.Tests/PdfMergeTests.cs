namespace PdfButcher.Tests
{
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.Model;
    using PdfButcher.Tests.Common;

    [TestFixture]
    public class PdfMergeTests
    {
        [Test]
        public void Should_merge_single_document()
        {
            using (var outputStream = new MemoryStream())
            {
                using (var pdfMerge = new PdfMerge(outputStream, true))
                {
                    pdfMerge.AddDocument(ResourcesHelper.OpenRead(ResourceFile.Pdf14SimplestWithMiscNames));
                }

                outputStream.Flush();
                outputStream.Position = 0;

                using (var outputDocument = PdfReader.ReadDocument(outputStream, true))
                {
                    outputDocument.PagesCount.Should().Be(1);

                    var indirectObjects = outputDocument.GetIndirectObjects().ToList();
                    indirectObjects.Should().HaveCount(6);
                }
            }
        }

        [Test]
        public void Should_merge_metadata()
        {
            using (var outputStream = new MemoryStream())
            {
                using (var pdfMerge = new PdfMerge(outputStream, true))
                {
                    pdfMerge.AddDocument(ResourcesHelper.OpenRead(ResourceFile.Pdf14SimplestWithXmp));
                }

                outputStream.Flush();
                outputStream.Position = 0;

                using (var outputDocument = PdfReader.ReadDocument(outputStream, true))
                {
                    outputDocument.PagesCount.Should().Be(1);

                    var catalog = outputDocument.Catalog.PdfObject.AsOrNull<PdfDictionary>();
                    var metadataReference = catalog.GetOrThrow(PdfNames.Metadata).AsOrNull<PdfReference>();
                    var metadataObject = outputDocument.GetIndirectObjectOrThrow(metadataReference.ReferencedObjectId);
                    var decodedStream = metadataObject.PdfStream.GetDecodedStream(metadataObject.PdfObject.AsOrThrow<PdfDictionary>(), outputDocument);
                    var decodedStreamContent = TestHelper.GetContent(decodedStream);
                    decodedStreamContent.Should().Contain("x:xmpmeta");
                }
            }
        }

        [Test]
        public void Should_merge_two_documents()
        {
            using (var outputStream = new MemoryStream())
            {
                using (var pdfMerge = new PdfMerge(outputStream, true))
                {
                    pdfMerge.AddDocument(ResourcesHelper.OpenRead(ResourceFile.Pdf14SimplestWithMiscNames));
                    pdfMerge.AddDocument(ResourcesHelper.OpenRead(ResourceFile.Pdf14SimplestWithMiscNames));
                }

                outputStream.Flush();
                outputStream.Position = 0;

                using (var outputDocument = PdfReader.ReadDocument(outputStream, true))
                {
                    outputDocument.PagesCount.Should().Be(2);

                    var indirectObjects = outputDocument.GetIndirectObjects().ToList();
                    indirectObjects.Should().HaveCount(10);
                }
            }
        }

        [Test]
        public void Should_merge_15_xref_stream_document_and_14_document()
        {
            using (var outputStream = new MemoryStream())
            {
                using (var pdfMerge = new PdfMerge(outputStream, true))
                {
                    pdfMerge.AddDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf14SimplestWithXmp));
                    pdfMerge.AddDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf15SimplestXRefInStream));
                }

                outputStream.Flush();
                outputStream.Position = 0;

                using (var outputDocument = PdfReader.ReadDocument(outputStream, true))
                {
                    outputDocument.PagesCount.Should().Be(2);
                    outputDocument.Version.Should().Be(15);

                    var objects = outputDocument.GetIndirectObjects().ToList();

                    // 16, the content stream object should not be merged.
                    objects.Count.Should().Be(16);

                    var catalog = outputDocument.Catalog.PdfObject.AsOrThrow<PdfDictionary>();
                    catalog.GetOrThrow(PdfNames.Metadata);
                    catalog.GetOrThrow(PdfNames.Pages);
                    catalog.GetOrThrow(PdfNames.Version);

                    var trailer = outputDocument.Trailer;

                    // Last object is 18, se Size is 18 + 1.
                    trailer.GetOrThrow(PdfNames.Size).AsOrThrow<PdfInteger>().Value.Should().Be(19);

                    var infoRef = trailer.GetOrThrow(PdfNames.Info).AsOrThrow<PdfReference>();
                    var infoObject = outputDocument.GetIndirectObjectOrThrow(infoRef.ReferencedObjectId);
                    var infoDictionary = infoObject.PdfObject.AsOrThrow<PdfDictionary>();

                    infoDictionary.GetOrThrow(PdfNames.CreationDate);
                    infoDictionary.GetOrThrow(PdfNames.Procuder);
                    infoDictionary.GetOrThrow(PdfNames.Creator);
                }
            }
        }
    }
}