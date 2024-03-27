namespace PdfButcher.Tests.Internals
{
    using System.IO;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals;

    [TestFixture]
    public class CommonHelpersTests
    {
        [Test]
        public void CopyStream_should_copy_stream()
        {
            var sourceStream = new MemoryStream(Enumerable.Repeat((byte)'A', 100).ToArray());
            var targetStream = new MemoryStream();

            CommonHelpers.CopyStream(sourceStream, targetStream, 10, 3);

            var targetArray = targetStream.ToArray();
            targetArray.Should().BeEquivalentTo(Enumerable.Repeat((byte)'A', 10).ToArray());
        }
    }
}