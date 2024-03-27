namespace PdfButcher.Tests.Internals.IO
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using FluentAssertions;
    using NUnit.Framework;
    using PdfButcher.Internals.IO;
    using PdfButcher.Internals.Model;

    [TestFixture]
    public class PdfLexerTests
    {
        [Test]
        public void Should_lex_xref_table()
        {
            const string XRefTable = @"xref
0 9
0000000000 65535 f
0000000023 00000 n
";

            var tokens = Lex(XRefTable);

            // xref\r\n
            tokens[0].TokenType.Should().Be(PdfTokenType.Keyword);
            tokens[0].KeywordValue.Should().Be("xref");
            tokens[1].TokenType.Should().Be(PdfTokenType.Newline);

            // 0 9\r\n
            tokens[2].IntegerValue.Should().Be(0);
            tokens[3].TokenType.Should().Be(PdfTokenType.Whitespace);
            tokens[4].IntegerValue.Should().Be(9);
            tokens[5].TokenType.Should().Be(PdfTokenType.Newline);

            // 0000000000 65535 f\r\n
            tokens[6].IntegerValue.Should().Be(0000000000);
            tokens[7].TokenType.Should().Be(PdfTokenType.Whitespace);
            tokens[8].IntegerValue.Should().Be(65535);
            tokens[9].TokenType.Should().Be(PdfTokenType.Whitespace);
            tokens[10].KeywordValue.Should().Be("f");
            tokens[11].TokenType.Should().Be(PdfTokenType.Newline);

            // 0000000023 00000 n\r\n
            tokens[12].IntegerValue.Should().Be(0000000023);
            tokens[13].TokenType.Should().Be(PdfTokenType.Whitespace);
            tokens[14].IntegerValue.Should().Be(00000);
            tokens[15].TokenType.Should().Be(PdfTokenType.Whitespace);
            tokens[16].KeywordValue.Should().Be("n");
            tokens[17].TokenType.Should().Be(PdfTokenType.Newline);
        }

        [Test]
        public void Should_lex_boolean_objects()
        {
            // 7.3.2 Boolean objects
            var tokens = Lex("true false");
            tokens[0].TokenType.Should().Be(PdfTokenType.True);
            tokens[2].TokenType.Should().Be(PdfTokenType.False);
        }

        [Test]
        public void Should_lex_unterminated_string_as_unknown_token()
        {
            var tokens = Lex("(111");
            tokens.Should().HaveCount(2);
            tokens[0].TokenType.Should().Be(PdfTokenType.Unknown);
            tokens[1].TokenType.Should().Be(PdfTokenType.EndOfFile);
        }

        [Test]
        public void Should_lex_comment_at_the_end()
        {
            var tokens = Lex("%This is comment");
            tokens.Should().HaveCount(2);
            tokens[0].TokenType.Should().Be(PdfTokenType.Comment);
            tokens[1].TokenType.Should().Be(PdfTokenType.EndOfFile);
        }

        [Test]
        public void Should_lex_comment()
        {
            var tokens = Lex("%This is comment\r\n");
            tokens.Should().HaveCount(3);
            tokens[0].TokenType.Should().Be(PdfTokenType.Comment);
            tokens[1].TokenType.Should().Be(PdfTokenType.Newline);
            tokens[2].TokenType.Should().Be(PdfTokenType.EndOfFile);
        }

        private List<PdfToken> Lex(string value)
        {
            var reader = new PdfLexer(new MemoryStream(Encoding.ASCII.GetBytes(value)));

            var tokens = new List<PdfToken>();

            while (true)
            {
                var token = reader.Next();
                tokens.Add(token);

                if (token.TokenType == PdfTokenType.EndOfFile)
                {
                    break;
                }
            }

            return tokens;
        }
    }
}