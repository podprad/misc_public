namespace PdfButcher.Internals.Model
{
    public enum PdfTokenType
    {
        Unknown,
        EndOfFile,
        Whitespace,
        Newline,
        Comment,
        Keyword,
        Name,
        LiteralString,
        HexString,
        Null,
        True,
        False,
        Integer,
        Real,
        DictionaryStart,
        DictionaryEnd,
        ArrayStart,
        ArrayEnd,
    }
}