namespace PdfButcher.Internals.Model
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.Filters;

    public class PdfStream
    {
        /// <summary>
        /// The start data position in <see cref="EncodedStream"/>.
        /// </summary>
        public long StartPosition { get; set; } = -1;

        /// <summary>
        /// Stream containing encoded, raw data. Usually direct reference to the input file stream.
        /// </summary>
        public Stream EncodedStream { get; set; }

        public PdfStream Clone(PdfDictionary owner, IPdfIndirectObjectsResolver indirectObjectsResolver)
        {
            var result = new PdfStream();

            var memoryStream = new MemoryStream();
            var copiedLength = CopyEncodedStream(owner, indirectObjectsResolver, memoryStream);
            if (copiedLength > 0)
            {
                result.EncodedStream = memoryStream;
                result.StartPosition = 0;
            }

            return result;
        }

        public long CopyEncodedStream(PdfDictionary owner, IPdfIndirectObjectsResolver pdfDocument, Stream targetStream)
        {
            var length = owner.Get(PdfNames.Length)?.ResolveValueOrThrow(pdfDocument).AsOrThrow<PdfInteger>();

            if (EncodedStream != null && length != null)
            {
                EncodedStream.Position = this.StartPosition;
                CommonHelpers.CopyStream(EncodedStream, targetStream, length.Value);
            }

            return length?.Value ?? 0;
        }

        /// <summary>
        /// Decodes the <see cref="EncodedStream"/> with using specified parameters.
        /// </summary>
        /// <param name="owner">The owner/parameters dictionary. Usually the <see cref="PdfDictionary"/> containing the stream. Should contain parameters like /Length or /Filter.</param>
        /// <param name="pdfDocument">Used to resolve values.</param>
        /// <returns>Decoded stream.</returns>
        public Stream GetDecodedStream(PdfDictionary owner, IPdfIndirectObjectsResolver pdfIndirectObjectsResolver)
        {
            var objectsToResolve = new List<PdfObject>();

            // Example:
            // /Filter [/FlateDecode /DCTDecode] /DecodeParms [null << /Quality 65 >>]
            var filterValue = owner.Get(PdfNames.Filter)?.ResolveValueOrThrow(pdfIndirectObjectsResolver);
            if (filterValue is PdfArray pdfArray)
            {
                objectsToResolve.AddRange(pdfArray.Values);
            }
            else if (filterValue is PdfObject pdfObject)
            {
                objectsToResolve.Add(pdfObject);
            }

            var decodeParameters = owner.Get(PdfNames.DecodeParms)?.ResolveValueOrThrow(pdfIndirectObjectsResolver);

            var filterNames = objectsToResolve.Select(f => f.ResolveValueOrThrow(pdfIndirectObjectsResolver)).OfType<PdfName>().ToList();

            var baseStream = this.EncodedStream;
            baseStream.Position = StartPosition;
            var chainedStream = baseStream;

            for (var index = 0; index < filterNames.Count; index++)
            {
                var filterName = filterNames[index];

                PdfDictionary filterParameters = null;

                if (decodeParameters is PdfArray arrayOfParameters)
                {
                    var parameters = arrayOfParameters.Values.ElementAtOrDefault(index);
                    filterParameters = parameters.ResolveValueOrThrow(pdfIndirectObjectsResolver).AsOrThrow<PdfDictionary>();
                }
                else if (decodeParameters is PdfDictionary singleParams)
                {
                    filterParameters = singleParams;
                }
                else
                {
                    filterParameters = new PdfDictionary();
                }

                // TODO: Support more encodings, implement elegant lookup.
                if (filterName.Value == PdfNames.FlateDecode)
                {
                    var flateFilter = new FlateFilter();
                    chainedStream = flateFilter.Decode(chainedStream, pdfIndirectObjectsResolver, filterParameters);
                }
                else
                {
                    throw new PdfException($"Unsupported filter {filterName}");
                }
            }

            return chainedStream;
        }
    }
}