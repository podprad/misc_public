namespace PDFSharpMetaIssue
{
    using System.Text;
    using PdfSharp.Pdf;
    using PdfSharp.Pdf.Advanced;

    public static class Program
    {
        public static void Main()
        {
            const string XmpToSet = @"<?xpacket begin=""ï»¿"" id=""W5M0MpCehiHzreSzNTczkc9d""?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"" x:xmptk=""Adobe XMP Core 6.1.10"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""""
        xmlns:xmp=""http://ns.adobe.com/xap/1.0/""
      xmp:CreatorTool=""Test""/>
  </rdf:RDF>
</x:xmpmeta>";

            var xmpBytes = new UTF8Encoding(false).GetBytes(XmpToSet);

            var filePath = "Pdf14Simplest.pdf";

            using (var document = PdfSharp.Pdf.IO.PdfReader.Open(filePath))
            {
                var catalog = document.Internals.Catalog;

                if (catalog.Elements.TryGetValue("/Metadata", out var oldMetadata))
                {
                    if (oldMetadata is PdfReference oldMetadataReference)
                    {
                        catalog.Elements.Remove("/Metadata");
                        document.Internals.RemoveObject(oldMetadataReference.Value);
                    }
                }

                var newMetadata = new PdfDictionary();
                newMetadata.CreateStream(xmpBytes);
                newMetadata.Elements.Add("/Type", new PdfName("/Metadata"));
                newMetadata.Elements.Add("/Subtype", new PdfName("/XML"));

                document.Internals.AddObject(newMetadata);

                catalog.Elements.Add("/Metadata", newMetadata.Reference);

                document.Save("Output.pdf");
            }
        }
    }
}