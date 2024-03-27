// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

var doc = PdfLexer.PdfDocument.Open(@"D:\Dev\misc\playgrounds\csharp\PdfButcher\PdfButcher.Tests\Resources\Pdfs\pdfjs\annotation-strikeout.pdf");

var entries = doc.XrefEntries;