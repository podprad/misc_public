namespace PdfButcher.Internals.Filters
{
    using System.IO;
    using System.IO.Compression;
    using PdfButcher.Internals.Model;
    using PdfButcher.Internals.ThirdParty.PdfJs;

    public class FlateFilter : IDecoder
    {
        private const int DefaultPredictor = 1;
        private const int DefaultColors = 1;
        private const int DefaultColumns = 1;
        private const int DefaultBPC = 8;

        public static FlateFilter Instance { get; } = new FlateFilter();

        public Stream Decode(Stream stream, IPdfIndirectObjectsResolver pdfIndirectObjectsResolver, PdfDictionary filterParameters)
        {
            // remove header
            stream.ReadByte();
            stream.ReadByte();

            var deflated = new DeflateStream(stream, CompressionMode.Decompress, false);

            if (filterParameters == null)
            {
                return deflated;
            }

            int predictor = filterParameters.GetAndResolveValueOrDefault(pdfIndirectObjectsResolver, PdfNames.Predictor, DefaultPredictor);
            if (predictor == 1)
            {
                return deflated;
            }

            int bpc = filterParameters.GetAndResolveValueOrDefault(pdfIndirectObjectsResolver, PdfNames.BitsPerComponent, DefaultBPC);
            int colors = filterParameters.GetAndResolveValueOrDefault(pdfIndirectObjectsResolver, PdfNames.Colors, DefaultColors);
            int columns = filterParameters.GetAndResolveValueOrDefault(pdfIndirectObjectsResolver, PdfNames.Columns, DefaultColumns);

            if (predictor == 2)
            {
                return TiffDecode(deflated, bpc, colors, columns);
            }
            else
            {
                return PngDecode(deflated, bpc, colors, columns);
            }
        }

        internal Stream TiffDecode(Stream stream, int bpc, int colors, int columns)
        {
            return new TiffStream(stream, bpc, colors, columns);
        }

        internal Stream PngDecode(Stream stream, int bpc, int colors, int columns)
        {
            return new PngStream(stream, bpc, colors, columns);
        }
    }
}