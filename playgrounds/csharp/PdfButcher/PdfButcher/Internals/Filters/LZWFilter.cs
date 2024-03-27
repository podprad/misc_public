// namespace PdfButcher.Filters
// {
//     using System.IO;
//     using PdfButcher.Filters.PdfJs;
//     using PdfButcher.Model;
//
//     public class LZWFilter : IDecoder
//     {
//         public static LZWFilter Instance { get; } = new LZWFilter();
//
//         public LZWFilter()
//         {
//         }
//
//         public Stream Decode(Stream stream, PdfDictionary filterParams)
//         {
//             var ec = filterParams?.Get("EarlyChange")?.AsOrThrow<PdfInteger>().Value;
//             if (ec != null)
//             {
//                 return FlateFilter.Instance.Decode(
//                     new LZWStream((int)ec.Value, stream),
//                     filterParams
//                 );
//             }
//             return new LZWStream(1, stream);
//         }
//     }
// }
//
//
//

