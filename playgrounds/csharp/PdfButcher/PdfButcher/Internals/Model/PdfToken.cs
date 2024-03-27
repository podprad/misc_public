namespace PdfButcher.Internals.Model
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using PdfButcher.Internals.IO;

    [DebuggerDisplay("{" + nameof(DebugDisplay) + "}")]
    public class PdfToken
    {
        private PdfToken(PdfTokenType tokenType, long position, byte[] rawData, int rawDataCount)
        {
            TokenType = tokenType;
            Position = position;
            Length = rawDataCount;

            if (tokenType == PdfTokenType.Name)
            {
                NameValue = EncodingHelper.DecodeName(rawData, rawDataCount, position);
            }
            else if (tokenType == PdfTokenType.Comment)
            {
                CommentValue = EncodingHelper.RawDecode(rawData, 0, rawDataCount);
            }
            else if (tokenType == PdfTokenType.HexString)
            {
                HexStringValue = EncodingHelper.DecodeHexCharacters(rawData, rawDataCount);
            }
            else if (tokenType == PdfTokenType.Keyword)
            {
                KeywordValue = EncodingHelper.RawDecode(rawData, 0, rawDataCount);
            }
            else if (tokenType == PdfTokenType.LiteralString)
            {
                LiteralStringValue = new byte[rawDataCount];
                Array.Copy(rawData, 0, LiteralStringValue, 0, rawDataCount);
            }
            else if (tokenType == PdfTokenType.Integer)
            {
                IntegerValue = EncodingHelper.DecodeNumber(rawData, rawDataCount);
            }
            else if (tokenType == PdfTokenType.Real)
            {
                var stringValue = EncodingHelper.RawDecode(rawData, 0, rawDataCount);
                DecimalValue = decimal.Parse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
        }

        public long Position { get; }

        public int Length { get; }

        public PdfTokenType TokenType { get; }

        public string NameValue { get; }

        public string CommentValue { get; }

        public string KeywordValue { get; }

        public long IntegerValue { get; }

        public decimal DecimalValue { get; }

        public byte[] LiteralStringValue { get; }

        public byte[] HexStringValue { get; }

        public string DebugDisplay
        {
            get
            {
                switch (TokenType)
                {
                    case PdfTokenType.Name:
                        return NameValue;

                    case PdfTokenType.Comment:
                        return CommentValue;

                    case PdfTokenType.HexString:
                        return EncodingHelper.EncodeHexString(HexStringValue);

                    case PdfTokenType.Keyword:
                        return KeywordValue;

                    case PdfTokenType.LiteralString:
                        return EncodingHelper.DecodeLiteralString(LiteralStringValue, LiteralStringValue.Length);

                    case PdfTokenType.Integer:
                        return IntegerValue.ToString(CultureInfo.InvariantCulture);

                    case PdfTokenType.Real:
                        return DecimalValue.ToString(CultureInfo.InvariantCulture);

                    default:
                        return $"{TokenType} pos={Position} length={Length}";
                }
            }
        }

        public static PdfToken Create(long position, int length, PdfTokenType tokenType, byte[] rawData)
        {
            return new PdfToken(tokenType, position, rawData, length);
        }

        public PdfToken RequireAnyKeyword(params string[] values)
        {
            RequireType(PdfTokenType.Keyword);

            if (!values.Contains(KeywordValue))
            {
                throw new PdfException($"Position {Position}. Expected {KeywordValue} to be one of the values {string.Join(",", values)}.");
            }

            return this;
        }

        public PdfToken RequireOneOfTypes(params PdfTokenType[] pdfTokenTypes)
        {
            foreach (var pdfTokenType in pdfTokenTypes)
            {
                if (this.TokenType == pdfTokenType)
                {
                    return this;
                }
            }

            var expectedString = string.Join("||", pdfTokenTypes.Select(g => g.ToString()));
            throw new PdfException($"Position {Position}. Expected {expectedString} but found {TokenType}.");
        }

        public PdfToken RequireType(PdfTokenType pdfTokenType)
        {
            return RequireOneOfTypes(pdfTokenType);
        }

        public PdfToken RequireKeyword(string value)
        {
            RequireType(PdfTokenType.Keyword);

            if (value != null && value != KeywordValue)
            {
                throw new PdfException($"Position {Position}. Expected {value} but found {KeywordValue}.");
            }

            return this;
        }
    }
}