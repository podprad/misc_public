// namespace PdfButcher.Filters
// {
//     using System;
//     using System.IO;
//     using PdfButcher.Filters.PdfJs;
//     using PdfButcher.Model;
//
//     public class CCITTFilter : IDecoder
//     {
//         private readonly Action<string> errorCallback;
//
//         public static CCITTFilter Instance { get; } = new CCITTFilter(null);
//
//         public CCITTFilter(Action<string> errorCallback = null)
//         {
//             this.errorCallback = errorCallback;
//         }
//
//         public Stream Decode(Stream stream, PdfDictionary filterParams)
//         {
//             return new CCITTStream(
//                 filterParams?.Get<PdfNumber>("K"),
//                 filterParams?.Get<PdfBoolean>("EndOfLine"),
//                 filterParams?.Get<PdfBoolean>("EndOfBlock"),
//                 filterParams?.Get<PdfBoolean>("BlackIs1"),
//                 filterParams?.Get<PdfBoolean>("EncodedByteAlign"),
//                 filterParams?.Get<PdfNumber>("Columns"),
//                 filterParams?.Get<PdfNumber>("Rows"),
//                 new BufferedStream(stream),
//                 errorCallback
//             );
//         }
//     }
// }

