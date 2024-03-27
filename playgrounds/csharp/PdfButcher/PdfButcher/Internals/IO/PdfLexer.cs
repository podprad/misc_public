namespace PdfButcher.Internals.IO
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfButcher.Internals.IO.LookAhead;
    using PdfButcher.Internals.Model;

    public class PdfLexer
    {
        private const int EndOfFile = -1;

        private readonly Stream _stream;

        private readonly ILookAheadReader<int> _la;

        private readonly BytesBuffer _buffer;

        private long _startPosition = -1;

        public PdfLexer(Stream stream) : this(stream, s => new BinaryLookAheadBuffer(s))
        {
        }

        public PdfLexer(Stream stream, Func<Stream, ILookAheadReader<int>> lookAheadReaderFactory)
        {
            _stream = stream;
            _la = lookAheadReaderFactory(stream);
            _buffer = new BytesBuffer();
        }

        public void Reset()
        {
            _la.Clear();
            _startPosition = -1;
        }

        public PdfToken Next()
        {
            _startPosition = _stream.Position - _la.AheadCount;

            var c = Read();

            // First lex tokens that do not need looking ahead.
            if (c == EndOfFile)
            {
                return CreateToken(PdfTokenType.EndOfFile, false);
            }

            if (c == '\n')
            {
                return CreateToken(PdfTokenType.Newline, false);
            }

            if (c == '%')
            {
                return LexComment();
            }

            if (c == '[')
            {
                return CreateToken(PdfTokenType.ArrayStart, false);
            }

            if (c == ']')
            {
                return CreateToken(PdfTokenType.ArrayEnd, false);
            }

            if (c == '/')
            {
                return LexName();
            }

            if (c == '(')
            {
                return LexLiteralString();
            }

            if (CommonHelpers.IsDigit(c))
            {
                return LexIntegerOrReal2();
            }

            var ahead = LookAhead();

            if (c == '\r' && ahead == '\n')
            {
                Read();

                return CreateToken(PdfTokenType.Newline, false);
            }

            // Must be after NewLine, because IsWhiteSpace also returns true for \n.
            if (CommonHelpers.IsWhiteSpace((byte)c))
            {
                return CreateToken(PdfTokenType.Whitespace, false);
            }

            if ((c == '+' || c == '-') && (ahead == '.' || CommonHelpers.IsDigit(ahead)))
            {
                // -. -0
                return LexIntegerOrReal1();
            }

            if (c == 't' && ahead == 'r' && LookAhead(2) == 'u' && LookAhead(3) == 'e')
            {
                Read(3);

                return CreateToken(PdfTokenType.True, false);
            }

            if (c == 'f' && ahead == 'a' && LookAhead(2) == 'l' && LookAhead(3) == 's' && LookAhead(4) == 'e')
            {
                Read(4);

                return CreateToken(PdfTokenType.False, false);
            }

            if (c == 'n' && ahead == 'u' && LookAhead(2) == 'l' && LookAhead(3) == 'l')
            {
                Read(3);

                return CreateToken(PdfTokenType.Null, false);
            }

            // Must be after true/false/null
            if (CommonHelpers.IsLetter(c))
            {
                return LexKeyword();
            }

            if (c == '<' && ahead == '<')
            {
                Read();

                return CreateToken(PdfTokenType.DictionaryStart, false);
            }

            if (c == '>' && ahead == '>')
            {
                Read();

                return CreateToken(PdfTokenType.DictionaryEnd, false);
            }

            if (c == '<' && (CommonHelpers.IsHexDigit(ahead) || ahead == '>'))
            {
                return LexHexString();
            }

            return CreateToken(PdfTokenType.Unknown, false);
        }

        private PdfToken LexName()
        {
            while (true)
            {
                var ahead = LookAhead();
                if (ahead == '#')
                {
                    // Encoded character
                    Read();

                    for (int i = 0; i < 2; i++)
                    {
                        if (CommonHelpers.IsHexDigit(LookAhead()))
                        {
                            Read();
                        }
                        else
                        {
                            // Error should be handled during decoding.
                            return CreateToken(PdfTokenType.Name, true);
                        }
                    }
                }
                else if (EncodingHelper.IsNameCharacter((char)ahead))
                {
                    // Specs says that range is 21-7E, but we should also exclude 
                    // Regular characters
                    Read();
                }
                else
                {
                    // Not in range, terminates.
                    return CreateToken(PdfTokenType.Name, true);
                }
            }
        }

        private PdfToken LexIntegerOrReal1()
        {
            var mode = PdfTokenType.Integer;

            if (LookAhead() == '.')
            {
                mode = PdfTokenType.Real;
            }

            Read();

            while (true)
            {
                if (LookAhead() == '.' && mode == PdfTokenType.Integer)
                {
                    mode = PdfTokenType.Real;
                    Read();
                }
                else if (CommonHelpers.IsDigit(LookAhead()))
                {
                    Read();
                }
                else
                {
                    return CreateToken(mode, true);
                }
            }
        }

        private PdfToken LexIntegerOrReal2()
        {
            var mode = PdfTokenType.Integer;

            while (true)
            {
                if (LookAhead() == '.' && mode == PdfTokenType.Integer)
                {
                    mode = PdfTokenType.Real;
                    Read();
                }
                else if (CommonHelpers.IsDigit(LookAhead()))
                {
                    Read();
                }
                else
                {
                    return CreateToken(mode, true);
                }
            }
        }

        private PdfToken LexKeyword()
        {
            while (true)
            {
                if (CommonHelpers.IsLetter(LookAhead()))
                {
                    Read();
                }
                else
                {
                    return CreateToken(PdfTokenType.Keyword, true);
                }
            }
        }

        private PdfToken LexComment()
        {
            while (true)
            {
                var ahead = LookAhead();
                if (ahead == '\r' || ahead == '\n' || ahead == EndOfFile)
                {
                    return CreateToken(PdfTokenType.Comment, true);
                }

                Read();
            }
        }

        private PdfToken LexLiteralString()
        {
            int parenthesesDepth = 0;

            while (true)
            {
                var ahead1 = LookAhead(1);
                if (ahead1 == EndOfFile)
                {
                    return CreateToken(PdfTokenType.Unknown, false);
                }

                if (ahead1 == '(')
                {
                    parenthesesDepth++;
                    Read();

                    continue;
                }

                if (ahead1 == ')' && parenthesesDepth > 0)
                {
                    parenthesesDepth--;
                    Read();

                    continue;
                }

                if (ahead1 == ')')
                {
                    Read();

                    return CreateToken(PdfTokenType.LiteralString, true);
                }

                if (ahead1 == '\\')
                {
                    Read();

                    var ahead2 = LookAhead();
                    if ("nrtbf()\\".Contains((char)ahead2))
                    {
                        Read();

                        continue;
                    }

                    if (CommonHelpers.IsOctDigit(ahead2))
                    {
                        Read();

                        var ahead3 = (char)LookAhead();
                        if (CommonHelpers.IsOctDigit(ahead3))
                        {
                            Read();

                            var ahead4 = (char)LookAhead();
                            if (CommonHelpers.IsOctDigit(ahead4))
                            {
                                Read();
                            }
                        }
                    }
                }
                else
                {
                    Read();
                }
            }
        }

        private PdfToken LexHexString()
        {
            while (true)
            {
                var ahead = LookAhead();
                if (CommonHelpers.IsHexDigit(ahead))
                {
                    Read();
                }
                else if (ahead == '>')
                {
                    Read();

                    return CreateToken(PdfTokenType.HexString, true);
                }
                else
                {
                    return CreateToken(PdfTokenType.Unknown, false);
                }
            }
        }

        private int LookAhead(int howFar = 1)
        {
            return _la.LookAhead(howFar);
        }

        private void Read(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Read();
            }
        }

        private int Read()
        {
            var read = _la.Read();

            AppendBuffer((byte)read);

            return read;
        }

        private void AppendBuffer(byte b)
        {
            _buffer.Append(b);
        }

        private PdfToken CreateToken(PdfTokenType tokenType, bool materialize)
        {
            var length = _buffer.Length;
            var enumerable = materialize ? _buffer.Buffer : null;
            var token = PdfToken.Create(_startPosition, length, tokenType, enumerable);

            _buffer.Clear();

            return token;
        }
    }
}