namespace PdfButcher.Tests.Internals.IO
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.Model;
    using PdfButcher.Tests.Common;

    /// <summary>
    /// Reader/writer tests, should test if we are conform with the spec.
    /// </summary>
    [TestFixture]
    public class PdfReaderWriterTests
    {
        [Test]
        [TestCase("true", true)]
        [TestCase("false", false)]
        public void ISO_7_3_2_Boolean_objects_read_write(string value, bool expectedValue)
        {
            var pdfObject = ReadObject(value);
            pdfObject.AsOrThrow<PdfBoolean>().Value.Should().Be(expectedValue);

            pdfObject.ToString().Should().Be(value);
        }

        [Test]
        [TestCase("123", 123)]
        [TestCase("43445", 43445)]
        [TestCase("+17", +17)]
        [TestCase("-98", -98)]
        [TestCase("0", 0)]
        public void ISO_7_3_3_Numeric_objects_integer_read(string value, long expectedValue)
        {
            ReadObject(value).AsOrThrow<PdfInteger>().Value.Should().Be(expectedValue);
        }

        [Test]
        [TestCase(123, "123")]
        [TestCase(43445, "43445")]
        [TestCase(+17, "17")]
        [TestCase(-98, "-98")]
        [TestCase(0, "0")]
        public void ISO_7_3_3_Numeric_objects_integer_write(long value, string expectedValue)
        {
            var pdfInteger = new PdfInteger(value);
            pdfInteger.ToString().Should().Be(expectedValue);
        }

        [Test]
        [TestCase("34.5", "34.5")]
        [TestCase("-3.62", "-3.62")]
        [TestCase("123.6", "123.6")]
        [TestCase("4.", "4.0")]
        [TestCase("-.002", "-0.002")]
        public void ISO_7_3_3_Numeric_objects_real_read(string value, string expectedValue)
        {
            var expectedDecimal = decimal.Parse(expectedValue, NumberStyles.Any, CultureInfo.InvariantCulture);

            var pdfReal = ReadObject(value).AsOrThrow<PdfReal>();
            pdfReal.Value.Should().Be(expectedDecimal);
        }

        [Test]
        [TestCase("34.5", "34.5")]
        [TestCase("-3.62", "-3.62")]
        [TestCase("123.6", "123.6")]
        [TestCase("4.", "4")]
        [TestCase("-.002", "-0.002")]
        public void ISO_7_3_3_Numeric_objects_real_write(string value, string expectedValue)
        {
            var decimalValue = decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);

            var pdfReal = new PdfReal(decimalValue);

            pdfReal.ToString().Should().Be(expectedValue);
        }

        [Test]
        [TestCase("(This is a string)", "This is a string")]
        [TestCase("(\\n \\r \\t \\b \\f \\( \\) \\\\)", "\n \r \t \b \f ( ) \\")]
        [TestCase("(\\7)", "\u0007")]
        [TestCase("(\\12)", "\u000A")]
        [TestCase("(\\123)", "\u0053")]
        [TestCase("(\\7 \\12 \\123)", "\u0007 \u000A \u0053")]
        [TestCase("(balanced ())", "balanced ()")]
        public void ISO_7_3_4_String_objects_literal_strings_read(string value, string expectedValue)
        {
            var pdfString = ReadObject(value).AsOrThrow<PdfString>();
            pdfString.Value.Should().Be(expectedValue);
        }

        [Test]
        public void ISO_7_3_4_String_objects_literal_strings_should_handle_control_chars()
        {
            // (ƒ as Position relative to the Page Margin)
            const string HexString = "28 83 20 61 73 20 50 6f 73 69 74 69 6f 6e 20 72 65 6c 61 74 69 76 65 20 74 6f 20 74 68 65 20 50 61 67 65 20 4d 61 72 67 69 6e 29";
            var bytes = TestHelper.HexStringToBytes(HexString);

            using (var stream = new MemoryStream(bytes))
            {
                var reader = new PdfReader(stream);
                var readObject = reader.ReadObject().AsOrThrow<PdfString>();

                using (var outStream = new MemoryStream())
                {
                    readObject.WriteTo(outStream);

                    var outArray = outStream.ToArray();
                    outArray.Should().BeEquivalentTo(bytes);
                }
            }
        }

        [Test]
        [TestCase("This is a string", "(This is a string)")]
        [TestCase("\n \r \t \b \f ( ) \\", "(\\n \\r \\t \\b \\f \\( \\) \\\\)")]
        [TestCase("\u0007", "(\a)")]
        [TestCase("\u000A", "(\\n)")]
        [TestCase("\u0053", "(S)")]
        [TestCase("\u0007 \u000A \u0053", "(\a \\n S)")]
        [TestCase("balanced ()", "(balanced \\(\\))")]
        public void ISO_7_3_4_String_objects_literal_strings_write(string value, string expectedValue)
        {
            var pdfString = new PdfString(value);

            var serialized = pdfString.ToString();

            serialized.Should().Be(expectedValue);
        }

        [Test]
        [TestCase("<901FA3>", "901FA3")]
        [TestCase("<901FA>", "901FA0")]
        [TestCase("<010F0>", "010F00")]
        [TestCase("<>", "")]
        public void ISO_7_3_4_String_objects_hex_strings_read_write(string value, string expectedValue)
        {
            var pdfHexString = ReadObject(value).AsOrThrow<PdfHexString>();
            var stringValue = pdfHexString.ToString();
            stringValue.Should().Be("<" + expectedValue + ">");
        }

        [Test]
        [TestCase("/A#42", "AB")]
        [TestCase("/text#2Fplain", "text/plain")]
        public void ISO_7_3_5_Name_objects_read(string value, string expectedValue)
        {
            var pdfName = ReadObject(value).AsOrThrow<PdfName>();
            var name = pdfName.Value;
            name.Should().Be(expectedValue);
        }

        [Test]
        [TestCase("/Name1", "Name1")]
        [TestCase("/ASomewhatLongerName", "ASomewhatLongerName")]
        [TestCase("/A;Name_With-Various***Characters?", "A;Name_With-Various***Characters?")]
        [TestCase("/1.2", "1.2")]
        [TestCase("/$$", "$$")]
        [TestCase("/@pattern", "@pattern")]
        [TestCase("/.notdef", ".notdef")]
        [TestCase("/Lime#20Green", "Lime Green")]
        [TestCase("/paired#28#29parentheses", "paired()parentheses")]
        [TestCase("/The_Key_of_F#23_Minor", "The_Key_of_F#_Minor")]
        [TestCase("/HXGHRK+Verdana", "HXGHRK+Verdana")]
        [TestCase("/AEPMCP+XXX,Bold-Identity-H", "AEPMCP+XXX,Bold-Identity-H")]
        [TestCase("/NXNFOW+EdiF-ux&gqGc1roWjej-00B", "NXNFOW+EdiF-ux&gqGc1roWjej-00B")]
        [TestCase("/text#2Fplain", "text/plain")]
        public void ISO_7_3_5_Name_objects_read_write(string value, string expectedValue)
        {
            var pdfName = ReadObject(value).AsOrThrow<PdfName>();
            var name = pdfName.Value;
            name.Should().Be(expectedValue);

            var serialized = pdfName.ToString();
            serialized.Should().Be(value);
        }

        [Test]
        [TestCase("[]", "[  ]")]
        [TestCase("[ 1 ]", "[ 1 ]")]
        [TestCase("[\r\n \t \t 1 \t \r\n]", "[ 1 ]")]
        [TestCase("[ 1 /Name (Hello World) ]", "[ 1 /Name (Hello World) ]")]
        public void ISO_7_3_6_Array_objects_read_write(string value, string expectedValue)
        {
            var array = ReadObject(value).AsOrThrow<PdfArray>();
            var stringValue = array.ToString();
            stringValue.Should().Be(expectedValue);
        }

        [Test]
        [TestCase("<<>>", "<<  >>")]
        [TestCase("<< /Age 23 >>", "<< /Age 23 >>")]
        [TestCase("<<\r\n /Age\r\n 23\r\n >>", "<< /Age 23 >>")]
        [TestCase("<< /Age 23 /Name (Julka) >>", "<< /Age 23 /Name (Julka) >>")]
        public void ISO_7_3_7_Dictionary_objects_read_write(string value, string expectedValue)
        {
            var dictionary = ReadObject(value).AsOrThrow<PdfDictionary>();
            var stringValue = dictionary.ToString();
            stringValue.Should().Be(expectedValue);
        }

        [Test]
        [TestCase("1 0 obj\r\n<< /Length 10 >>\r\nstream\r\nHelloWorld\r\n", "HelloWorld")]
        public void ISO_7_3_8_Stream_objects(string value, string expectedStreamContent)
        {
            var indirectObject = ReadIndirectObject(value);
            var pdfStream = indirectObject.PdfStream;
            pdfStream.Should().NotBeNull();
            pdfStream.StartPosition.Should().Be(35);
            pdfStream.EncodedStream.Should().NotBeNull();
        }

        [Test]
        [Ignore("TODO")]
        public void ISO_7_3_8_Stream_objects_in_external_file()
        {
        }

        [Test]
        public void ISO_7_3_9_Null_object()
        {
            var pdfNull = ReadObject("null").AsOrThrow<PdfNull>();
            pdfNull.ToString().Should().Be("null");
        }

        [Test]
        [TestCase("12 5 obj\r\n(Hello)\r\nendobj\r\n", "(Hello)", 12, 5)]
        [TestCase("1 5 obj\r\n1\r\nendobj\r\n", "1", 1, 5)]
        [TestCase("1 0 obj\r\n<< /Age 10 >>\r\nendobj\r\n", "<< /Age 10 >>", 1, 0)]
        public void ISO_7_3_10_Indirect_objects(string value, string expectedContent, int expectedNumber, int expectedRevision)
        {
            var parsedObject = ReadIndirectObject(value);
            parsedObject.Id.Id.Should().Be(expectedNumber);
            parsedObject.Id.Revision.Should().Be(expectedRevision);
            parsedObject.PdfObject.ToString().Should().Be(expectedContent);
        }

        [Test]
        public void ISO_7_3_10_Indirect_objects_may_reside_in_object_streams()
        {
            Assert.Pass("Satisfied by test" + nameof(ISO_7_5_8_Cross_reference_streams));
        }

        [Test]
        public void ISO_7_3_10_Indirect_objects_may_have_values_as_references()
        {
            using (var document = ReadDocument(ResourceFile.Pdf14LengthInSeparateObject))
            {
                var indirectObject = document.GetIndirectObjectOrThrow(new PdfObjectId(6, 0));
                var lengthReference = indirectObject.PdfObject.AsOrThrow<PdfDictionary>().GetOrThrow(PdfNames.Length);
                var length = lengthReference.ResolveValueOrThrow(document).AsOrThrow<PdfInteger>();
                length.Value.Should().Be(44);
            }
        }

        [Test]
        [Ignore("TODO")]
        public void ISO_7_4_2_ASCIIHexDecode_filter()
        {
        }

        [Test]
        [Ignore("TODO")]
        public void ISO_7_4_3_ASCII85Decode_filter()
        {
        }

        [Test]
        [Ignore("TODO")]
        public void ISO_7_4_4_LZWDecode_filter()
        {
        }

        [Test]
        public void ISO_7_4_4_FlateDecode_filter()
        {
            using (var document = ReadDocument(ResourceFile.Pdf14Inkscape))
            {
                var pageContentId = new PdfObjectId(4, 0);
                var objectWithStream = document.GetIndirectObjectOrThrow(pageContentId);
                var stream = objectWithStream.PdfStream.GetDecodedStream(objectWithStream.PdfObject.AsOrThrow<PdfDictionary>(), document);

                const string ExpectedContent = "1 0 0 -1 0 841.889771 cm\nq\n0 0 0 rg /a0 gs\nBT\n29.999904 0 0 -29.999904 59.998155 121.655009 Tm\n/f-0-0 1 Tf\n[(Hello W)49(orld)]TJ\nET\nQ\n";

                using (var reader = new StreamReader(stream, PdfConstants.PdfFileEncoding, false, 1024, true))
                {
                    var content = reader.ReadToEnd();
                    content.Should().Be(ExpectedContent);
                }
            }
        }

        [Test]
        [Ignore("TODO")]
        public void ISO_7_5_2_File_header_should_be_followed_by_binary_data_indicator()
        {
            // If a PDF file contains binary data, as most do (see 7.2, "Lexical conventions"), the header line shall be 
            // immediately followed by a comment line containing at least four binary characters–that is, characters 
            // whose codes are 128 or greater. This ensures proper behaviour of file transfer applications that 
            // inspect data near the beginning of a file to determine whether to treat the file’s contents as text or as 
            // binary
        }

        [Test]
        public void ISO_7_5_4_Cross_reference_table()
        {
            Assert.Pass("Covered by misc document reading.");
        }

        [Test]
        public void ISO_7_5_6_Incremental_updates_PDF14_object_changed_1()
        {
            // Incremental update updates text Hello World => HELLO WORLD!
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf14SimplestIncrementalUpdate)))
            {
                var allObjects = document.GetIndirectObjects().ToList();
                allObjects.Should().HaveCount(6);

                var contentObject = document.GetIndirectObjectOrThrow(new PdfObjectId(6, 1));
                var decodedStream = contentObject.PdfStream.GetDecodedStream(contentObject.PdfObject.AsOrThrow<PdfDictionary>(), document);
                var contentDump = TestHelper.GetContent(decodedStream, 44);
                contentDump.Should().Contain("HELLO WORLD");
            }
        }

        [Test]
        public void ISO_7_5_6_Incremental_updates_PDF14_object_changed_2()
        {
            // Incremental update adds annotation to page.
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf17WordIncrementalUpdate)))
            {
                var pageObject = document.GetIndirectObjectOrThrow(new PdfObjectId(3, 0));
                var pageDictionary = pageObject.PdfObject.AsOrThrow<PdfDictionary>();
                var annotationsArray = pageDictionary.GetOrThrow(PdfNames.Annots).AsOrThrow<PdfArray>();
                var annotationsReference = annotationsArray.Values.First().AsOrThrow<PdfReference>();

                var annotationObject = document.GetIndirectObjectOrThrow(annotationsReference.ReferencedObjectId);
                var annotationDictionary = annotationObject.PdfObject.AsOrThrow<PdfDictionary>();
                var content = annotationDictionary.GetOrThrow(PdfNames.Contents).AsOrThrow<PdfString>();
                content.Value.Should().Be("Hello Annotation!");
            }
        }

        [Test]
        [Ignore("broken file, xref table is not cross reference stream, PDF readers like chrome see 1 page instead of 2 pages.")]
        public void ISO_7_5_8_2_Incremental_updates_PDF15_in_cross_reference_streams_1()
        {
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf17SimplestIncrementalUpdateByThirdPartyToolOptimized)))
            {
                var contentStream = document.GetIndirectObjectOrThrow(new PdfObjectId(13, 0));
                var indirects = document.Reader.ReadIndirectObjectsFromObjectsStream(document, contentStream).ToList();
            }
        }

        [Test]
        public void ISO_7_5_8_2_Should_merge_multiple_xref_tables()
        {
            // Pdf17SimplestLinearized contains /Prev which links to previous stream xref table
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf17SimplestLinearized)))
            {
                var objects = document.GetIndirectObjects().ToList();
                objects.Should().HaveCount(14);

                objects.Count(g => g.Removed).Should().Be(0);
            }
        }

        [Test]
        public void ISO_7_5_8_2_Incremental_updates_should_allow_removing_objects()
        {
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf14SimplestIncrementalUpdateObjectRemoved)))
            {
                var indirectObjects = document.GetIndirectObjects().ToList();
                var removedObjects = indirectObjects.Where(g => g.Removed).ToList();
                removedObjects.Should().HaveCount(1);
            }
        }

        [Test]
        public void ISO_7_5_8_4_Compatibility_with_applications_that_do_not_support_compressed_reference_streams_aka_hybrid_xrefs()
        {
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf17Word)))
            {
                var indirectObjects = document.GetIndirectObjects().ToList();
                indirectObjects.Should().HaveCount(28);

                var removedObjects = indirectObjects.Where(g => g.Removed).ToList();
                removedObjects.Should().HaveCount(0);
            }
        }

        [Test]
        public void ISO_7_5_8_Cross_reference_streams()
        {
            using (var document = PdfReader.ReadDocument(ResourcesHelper.GetCustomPdfResourcePath(ResourceFile.Pdf15SimplestXRefInStream)))
            {
                document.Version.Should().Be(15);
                document.Catalog.Should().NotBeNull();
                document.PagesCount.Should().Be(1);
                var allObjects = document.GetIndirectObjects().OrderBy(g => g.Id.Id).ToList();
                allObjects.Count.Should().Be(11);

                var objectWithFirstId = allObjects.First();
                objectWithFirstId.Id.Id.Should().Be(1);

                var objectWithLastId = allObjects.Last();
                objectWithLastId.Id.Id.Should().Be(13);
            }
        }

        [Test]
        public void ISO_7_7_3_PageTree()
        {
            // Prepare test document, where /Pages is a tree containing another /Pages. Example:
            // 37 0 obj
            // <</Type/Pages/Count 14/Kids[23 0 R 56 0 R]>>
            // endobj
            // 23 0 obj
            // <</Type/Pages/Count 10/Kids[18 0 R 28 0 R 29 0 R 30 0 R 31 0 R 32 0 R 33 0 R 34 0 R 35 0 R 36 0 R]/Parent 37 0 R>>
            // endobj
            // 56 0 obj
            // <</Type/Pages/Count 4/Kids[66 0 R 67 0 R 68 0 R 69 0 R]/Parent 37 0 R>>
            // endobj
            Assert.Fail();
        }

        [Test]
        public void Should_parse_Pdf14SimplestWithMiscNames()
        {
            using (var document = ReadDocument(ResourceFile.Pdf14SimplestWithMiscNames))
            {
                document.Version.Should().Be(14);
                document.XRefTable.Count.Should().Be(7);
                document.Trailer.ToString().Should().Be("<< /Root 1 0 R /Size 7 >>");

                var rootObjects = document.GetIndirectObjects().ToList();
                rootObjects.Should().HaveCount(6);
                rootObjects[0].PdfObject.ToString().Should().Be("<< /Type /Catalog /Pages 2 0 R >>");
                rootObjects[1].PdfObject.ToString().Should().Be("<< /Type /Pages /Kids [ 3 0 R ] /Count 1 >>");
                rootObjects[2].PdfObject.ToString().Should().Be("<< /Type /Page /Parent 2 0 R /Resources 4 0 R /MediaBox [ 0 0 500 800 ] /Contents 6 0 R >>");
                rootObjects[3].PdfObject.ToString().Should().Be("<< /Font << /F1 5 0 R >> >>");
                rootObjects[4].PdfObject.ToString().Should().Be("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
                rootObjects[5].PdfObject.ToString().Should().Be("<< /Length 44 >>");

                var pages = document.GetPages().ToList();
                pages.Should().HaveCount(1);
                pages[0].Should().Be(rootObjects[2]);

                var dictionary = rootObjects[5];
                var contentStream = dictionary.PdfStream;
                contentStream.Should().NotBeNull();
                contentStream.StartPosition.Should().Be(363);
                contentStream.EncodedStream.Should().NotBeNull();
            }
        }

        [Test]
        public void Should_parse_Pdf14Inkscape()
        {
            using (var document = ReadDocument(ResourceFile.Pdf14Inkscape))
            {
                document.Version.Should().Be(14);
                document.PagesCount.Should().Be(1);

                document.GetIndirectObjects().ToList();

                var documentInfo = document.GetIndirectObjectOrThrow(new PdfObjectId(12, 0)).PdfObject.AsOrThrow<PdfDictionary>();
                var producer = documentInfo.GetOrThrow(PdfNames.Procuder).AsOrThrow<PdfString>();
                producer.Value.Should().Be("cairo 1.17.4 (https://cairographics.org)");

                var creator = documentInfo.GetOrThrow(PdfNames.Creator).AsOrThrow<PdfHexString>();
                creator.ToString().Should().Be("<FEFF0049006E006B0073006300610070006500200031002E0030002E0032002D00320020002800680074007400700073003A002F002F0069006E006B00730063006100700065002E006F007200670029>");

                var creationDate = documentInfo.GetOrThrow(PdfNames.CreationDate).AsOrThrow<PdfString>();
                creationDate.Value.Should().Be("D:20230906141918+02'00");
            }
        }

        [Test]
        public void Should_parse_pdf_with_multiple_xref_groups()
        {
            using (var document = ReadDocument(ResourceFile.Pdf14XRefGroups))
            {
                document.Version.Should().Be(14);
                document.PagesCount.Should().Be(2);
                document.XRefTable.Count.Should().Be(11);
            }
        }

        private T ReadObject<T>(string value, Func<PdfReader, T> parseCallback)
        {
            using (var stream = new MemoryStream(PdfConstants.PdfFileEncoding.GetBytes(value)))
            {
                var reader = new PdfReader(stream);
                var result = parseCallback(reader);

                return result;
            }
        }

        private PdfReaderDocument ReadDocument(ResourceFile resourceFile)
        {
            return PdfReader.ReadDocument(ResourcesHelper.OpenRead(resourceFile), false);
        }

        private PdfObject ReadObject(string value)
        {
            return ReadObject(value, parser => parser.ReadObject());
        }

        private PdfIndirectObject ReadIndirectObject(string value)
        {
            return ReadObject(value, parser => parser.ReadIndirectObject());
        }
    }
}