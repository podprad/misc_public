namespace PdfButcher.Tests.Internals.IO
{
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.Model;

    [TestFixture]
    public class PdfObjectsRenumberToolTests
    {
        private PdfIndirectObject _indirect1;
        private PdfIndirectObject _indirect3;
        private PdfIndirectObject _indirect2;
        private PdfIndirectObjectsGroup _group;
        private PdfObjectsRenumberTool _tool;
        private PdfReference _object1;
        private PdfReference _object2;
        private PdfReference _object3;

        [SetUp]
        public void SetUp()
        {
            _object1 = new PdfReference(new PdfObjectId(2, 0));
            _object2 = new PdfReference(new PdfObjectId(3, 0));
            _object3 = new PdfReference(new PdfObjectId(1, 0));

            _indirect1 = new PdfIndirectObject(new PdfObjectId(1, 0), _object1);
            _indirect2 = new PdfIndirectObject(new PdfObjectId(2, 0), _object2);
            _indirect3 = new PdfIndirectObject(new PdfObjectId(3, 0), _object3);

            _group = new PdfIndirectObjectsGroup(new[] { _indirect1, _indirect2, _indirect3, });

            _tool = new PdfObjectsRenumberTool();
        }

        [Test]
        public void Should_rename_easy_case()
        {
            var max = _tool.RenumberObjects(_group, new PdfObjectId(5, 0), new[] { _indirect2, _indirect3, });
            max.Id.Should().Be(7);

            _indirect1.Id.Id.Should().Be(7);
            _indirect2.Id.Id.Should().Be(5);
            _indirect3.Id.Id.Should().Be(6);

            _object1.ReferencedObjectId.Id.Should().Be(5);
            _object2.ReferencedObjectId.Id.Should().Be(6);
            _object3.ReferencedObjectId.Id.Should().Be(7);
        }

        [Test]
        public void Should_rename_overlapping()
        {
            var max = _tool.RenumberObjects(_group, new PdfObjectId(2, 0), new[] { _indirect3, _indirect2, _indirect1, });
            max.Id.Should().Be(4);

            _indirect3.Id.Id.Should().Be(2);
            _indirect2.Id.Id.Should().Be(3);
            _indirect1.Id.Id.Should().Be(4);

            // obj 3 references obj 1
            _object3.ReferencedObjectId.Id.Should().Be(_indirect1.Id.Id);

            // obj 2 references obj 3
            _object2.ReferencedObjectId.Id.Should().Be(_indirect3.Id.Id);

            // obj 1 references obj 2
            _object1.ReferencedObjectId.Id.Should().Be(_indirect2.Id.Id);
        }
    }
}